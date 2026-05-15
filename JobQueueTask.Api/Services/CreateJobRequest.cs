using System.Text.Json.Serialization;

namespace JobQueueTask.Api.Services;

public sealed record Payload(string ReceipientEmail, string ReportId);

public sealed record CreateJobRequest(JobType Type, Payload Payload, int MaxRetries = 3);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobType
{
    ExportCsv,
    SendReport,
}
