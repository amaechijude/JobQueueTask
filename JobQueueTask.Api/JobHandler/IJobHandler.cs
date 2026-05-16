namespace JobQueueTask.Api.JobHandler;

public interface IJobHandler
{
    Task<string> ExecuteAsync(Guid jobId, CancellationToken ct);
}
