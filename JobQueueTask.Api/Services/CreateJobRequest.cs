using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace JobQueueTask.Api.Services;

public sealed record Payload([EmailAddress] string ReceipientEmail, string ReportId);

public sealed record CreateJobRequest(
    JobType Type,
    Payload Payload,
    [Range(1, 5)] int MaxRetries = 3
);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobType
{
    ExportCsv,
    SendReport,
}
