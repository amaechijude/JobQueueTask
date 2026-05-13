namespace JobQueueTask.Api.JobHandler;

public sealed class JobHandler : IJobHandler
{
    public string JobType => throw new NotImplementedException();

    public Task<string> ExecuteAsync(string payload, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    private Task<string> ExportCsvHandler(string payload, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    private Task<string> SendReportHandler(string payload, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
