namespace JobQueueTask.Api.Services;

public interface IJobService
{
    Task CancelJobAsync(Guid id, CancellationToken cancellationToken);
    Task<ApiResponse<string>> CreateJobAsync(
        CreateJobRequest request,
        CancellationToken cancellationToken
    );
    Task<ApiResponse<GetJobResponse>> GetJobByIdAsync(Guid id, CancellationToken ct);
    Task<ApiResponse<ListJobsResponse>> ListJobsAsync(
        ListJobsRequest request,
        CancellationToken cancellationToken
    );
    Task<ApiResponse<ListJobStatistics>> ListJobStatisticsAsync(CancellationToken ct);
}
