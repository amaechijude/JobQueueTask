using JobQueueTask.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JobQueue.Api.BackgroundWorkers;

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
    }

    private async Task ProcessOrphanedJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<JobDbContext>();

        DateTimeOffset orphanedThreshold = DateTimeOffset.UtcNow.AddMinutes(-5);

        List<Guid> jobIds = await dbContext
            .Jobs.AsNoTracking()
            .Where(j =>
                j.Status == JobStatus.Running
                && j.StartedAt != null
                && j.StartedAt <= orphanedThreshold
                && j.RetryCount <= j.MaxRetries
            )
            .OrderBy(j => j.Id)
            .Take(50)
            .Select(j => j.Id)
            .ToListAsync(cancellationToken);

        if (jobIds.Count > 0)
            return;

        await dbContext
            .Jobs.Where(j => jobIds.Contains(j.Id))
            .ExecuteUpdateAsync(
                setters =>
                {
                    setters.SetProperty(j => j.Status, JobStatus.Failed);
                },
                cancellationToken
            );
    }
}
