using JobQueueTask.Api.Entities;
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


        List<Guid> jobIds = await dbContext
            .Jobs.AsNoTracking()
            .Where(j => j.Status == JobStatus.Running)
            .TagWithCallSite()
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
