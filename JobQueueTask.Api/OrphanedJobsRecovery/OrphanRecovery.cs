using JobQueueTask.Api.Entities;
using JobQueueTask.Api.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JobQueueTask.Api.OrphanedJobsRecovery;

public sealed class OrphanRecovery(
    IOptions<JobQueueOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<OrphanRecovery> logger
) : BackgroundService
{
    private readonly int _recoveryInterval =
        options.Value.RecoveryIntervalSeconds > 0 ? options.Value.RecoveryIntervalSeconds : 60;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(_recoveryInterval));
        try
        {
            while (
                !stoppingToken.IsCancellationRequested
                && await timer.WaitForNextTickAsync(stoppingToken)
            )
            {
                await ProcessOrphanedJobsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Orphan recovery is shutting dowm");
        }
        catch (Exception ex)
        {
            logger.LogError("Exception occured in Orphan recovery loop {message}", ex.Message);

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessOrphanedJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<JobDbContext>();
        var jobQueue = scope.ServiceProvider.GetRequiredService<IJobQueue>();

        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-options.Value.OrphanTimeoutMinutes);

        var orphanedJobs = await dbContext
            .Jobs.Where(e =>
                (e.StartedAt ?? DateTimeOffset.MinValue) < cutoff && e.Status == JobStatus.Running
            )
            .TagWithCallSite()
            .OrderBy(j => j.Id)
            .Take(50)
            .ToListAsync(cancellationToken);

        var count = orphanedJobs.Count;

        if (count == 0)
            return;

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Found {Count} orphaned jobs to recover.", orphanedJobs.Count);

        HashSet<Guid> IdsToEnque = new(count);

        foreach (var job in orphanedJobs)
        {
            job.ResolveFailedJob();

            if (job.Status == JobStatus.Pending)
                IdsToEnque.Add(job.Id);
        }
        await jobQueue.EnqueueAsync(IdsToEnque, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
