namespace JobQueueTask.Api.JobHandler;

public interface IJobHandler
{
    string JobType { get; }
    Task<string> ExecuteAsync(string payload, CancellationToken ct);
}
