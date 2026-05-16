using System.Runtime.CompilerServices;
using JobQueueTask.Api.Entities;
using JobQueueTask.Api.Redis;

namespace JobQueueTask.Api.JobHandler;

public class SendReportHandler(
    JobDbContext dbcontext,
    IJobQueue jobQueue,
    ILogger<SendReportHandler> logger
) : IJobHandler
{
    public async Task<string> ExecuteAsync(Guid jobId, CancellationToken ct)
    {
        var job = await dbcontext.Jobs.FindAsync([jobId], ct);

        if (job is null || job.Status == JobStatus.Pending)
            return string.Empty;

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation(
                "Beging processing of job {jobId} with jobtype {jobtype}",
                jobId,
                job.Type
            );

        // Simulate payload deserialization
        var request = PayloadSerializer.Deserialize<ExportCsvRequest>(job.Payload);
        if (request is null)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Invalid or empty payload on job {jobid}", job.Id);
            return string.Empty;
        }

        var (success, result) = await DoWorkAsync(request, ct);
        if (success)
        {
            job.Complete(result);
        }
        else
        {
            job.ResolveFailedJob();
            await jobQueue.EnqueueAsync(job.Id, ct);
        }
        await dbcontext.SaveChangesAsync(ct);

        return string.Empty;
    }

    private static async Task<(bool success, string result)> DoWorkAsync(
        ExportCsvRequest request,
        CancellationToken ct
    )
    {
        // Simulate work duration
        await Task.Delay(1000, ct);

        bool success = Random.Shared.Next(0, 6) != 5;

        return (success, PayloadSerializer.Serialize(request));
    }
}

public sealed record SendReportRequest(string ReciepientEmail, string ReportId);

public sealed record SendReportResponse(string SentAt, string Status);
