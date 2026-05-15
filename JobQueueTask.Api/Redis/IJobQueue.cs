namespace JobQueueTask.Api.Redis;

public interface IJobQueue
{
    Task EnqueueAsync(Guid jobId, CancellationToken cancellationToken);
    Task EnqueueAsync(IEnumerable<Guid> jobIds, CancellationToken cancellationToken);
    Task<Guid?> DequeueAsync(CancellationToken cancellationToken);
}
