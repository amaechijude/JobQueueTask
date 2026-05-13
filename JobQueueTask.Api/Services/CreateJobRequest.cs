namespace JobQueueTask.Api.Services;

public sealed record Payload(string ReceipientEmail, string ReportId);

public sealed record CreateJobRequest(string Type, Payload Payload, int MaxRetries = 3);
