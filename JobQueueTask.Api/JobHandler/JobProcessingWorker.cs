using JobQueueTask.Api.Entities;
using JobQueueTask.Api.Redis;

namespace JobQueueTask.Api.JobHandler;

public sealed class JobProcessingWorker(
    IServiceScopeFactory scopeFactory,
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
                // Graceful shutdown requested
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

        var jobQueue = scope.ServiceProvider.GetRequiredService<IJobQueue>();
        var dbContext = scope.ServiceProvider.GetRequiredService<JobDbContext>();

        // 1. Pull the next Job ID from Redis (assuming IJobQueue has a Dequeue method)
        // If your Dequeue doesn't block, it might return null when empty.
        var jobId = await jobQueue.DequeueAsync(ct);
        if (jobId is null || jobId == Guid.Empty)
        {
            await Task.Delay(1000, ct); // Prevent CPU spamming if queue is empty
            return;
        }

        // 2. Fetch the job details from the Database
        var job = await dbContext.Jobs.FindAsync([jobId], ct);
        if (job == null)
        {
            logger.LogWarning("Job {JobId} popped from queue but not found in DB.", jobId);
            return;
        }

        try
        {
            // 3. Dynamically resolve the correct handler using .NET 8 Keyed Services
            var handler = scope.ServiceProvider.GetRequiredKeyedService<IJobHandler>(job.Type);

            // 4. Execute the specific job logic
            var result = await handler.ExecuteAsync(job.Payload, ct);

            // 5. Update Job status upon success
            job.AddResult(result);
            // job.Status = ...; (e.g., JobStatus.Completed)
            // job.CompletedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation(
                    "Job processing was cancelled for Job {JobId} due to shutdown.",
                    jobId
                );
            throw; // Rethrow to let the outer loop handle graceful shutdown
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Execution failed for Job {JobId}.", jobId);

            // Handle failure: Update job status to Failed, increment RetryCount, or requeue
            // await dbContext.SaveChangesAsync(ct);
        }
    }
}
