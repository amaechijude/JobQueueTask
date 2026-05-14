namespace JobQueueTask.Api.JobHandler;

public interface IJobHandler
{
    Task<string> ExecuteAsync(string payload, CancellationToken ct);
}
