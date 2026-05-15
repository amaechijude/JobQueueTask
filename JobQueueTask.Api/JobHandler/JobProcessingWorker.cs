using JobQueueTask.Api.Entities;
using JobQueueTask.Api.Redis;
using Microsoft.EntityFrameworkCore;

namespace JobQueueTask.Api.JobHandler;

public sealed class JobProcessingWorker(
    IServiceScopeFactory scopeFactory,
    IJobQueue jobQueue,
    ILogger<JobProcessingWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Job Processing Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNextJobAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "A fatal error occurred in the job processing loop.");
                // Backoff to prevent tight looping on persistent connection failures
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcessNextJobAsync(CancellationToken ct)
    {
        // Create a new scope for each processing attempt
        await using var scope = scopeFactory.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<JobDbContext>();

        // 1. Pull the next Job ID from Redis
        // If your Dequeue doesn't block, it might return null when empty.
        var jobId = await jobQueue.DequeueAsync(ct);
        if (jobId is null || jobId == Guid.Empty)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct); // Prevent CPU spamming if queue is empty
            return;
        }

        // 2. Fetch the job details from the Database
        var job = await dbContext.Jobs.FindAsync([jobId], ct);
        if (job is null)
        {
            logger.LogWarning("Job {JobId} popped from queue but not found in DB.", jobId);
            return;
        }
        if (job.Status != JobStatus.Pending)
        {
            logger.LogWarning("job already running or completed.");
            return;
        }

        // transition job to running
        job.StartRunning();

        try
        {
            // 3. Dynamically resolve the correct handler using Keyed Services
            var handler = scope.ServiceProvider.GetRequiredKeyedService<IJobHandler>(job.Type);

            // 4. Execute the specific job logic
            var result = await handler.ExecuteAsync(job.Payload, ct);

            // 5. Update Job status
            if (string.IsNullOrWhiteSpace(result)) // fail
            {
                job.RequeueAsPending();
                await jobQueue.EnqueueAsync(job.Id, ct);
            }
            else
            {
                job.Complete(result);
            }
        }
        catch (OperationCanceledException)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation(
                    "Job processing was cancelled for Job {JobId} due to shutdown.",
                    jobId
                );
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                "An error ocuured while processing job with id {id}. {message}",
                job.Id,
                ex.Message
            );
            job.RequeueAsPending();
            await jobQueue.EnqueueAsync(job.Id, ct);
        }
        finally
        {
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
