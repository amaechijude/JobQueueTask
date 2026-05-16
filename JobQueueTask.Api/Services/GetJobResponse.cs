using JobQueueTask.Api.Entities;

namespace JobQueueTask.Api.Services;

public sealed record ListJobsRequest(JobStatus? Status, int PageNumber = 1, int PageSize = 20);

public sealed record ListJobsResponse(IEnumerable<GetJobResponse> Jobs, bool HasNext);

public sealed record CancelJobResponse(string Status = "Cancelled");

public sealed record ListJobStatistics(IEnumerable<Stat> Stats);

public sealed record Stat(string Type, int Count);

public sealed record JobResult(DateTimeOffset? SentAt, JobStatus Status);

public sealed record GetJobResponse(
    Guid Id,
    string Type,
    JobStatus Status,
    object? Result,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    int RetryCount
);
