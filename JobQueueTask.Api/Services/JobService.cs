using JobQueueTask.Api.Entities;
using JobQueueTask.Api.JobHandler;
using JobQueueTask.Api.Redis;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace JobQueueTask.Api.Services;

public sealed class JobService(JobDbContext context, IJobQueue jobQueue, ILogger<JobService> logger)
    : IJobService
{
    public async Task<ApiResponse<string>> CreateJobAsync(
        CreateJobRequest request,
        CancellationToken cancellationToken
    )
    {
        var payload = PayloadSerializer.Serialize(request.Payload);
        var newJob = Job.Create(
            type: request.Type.ToString(),
            maxRetries: request.MaxRetries,
            payload: payload
        );
        context.Jobs.Add(newJob);
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            await jobQueue.EnqueueAsync(newJob.Id, cancellationToken);
        }
        catch (RedisConnectionException ex)
        {
            logger.LogError("Unable to connect to redis. {message}", ex.Message);
        }
        catch (RedisTimeoutException ex)
        {
            logger.LogError(
                "Redis request timed out on {date}  {message}",
                DateTimeOffset.UtcNow,
                ex.Message
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                "Error occured while adding {jobId} to the queue {message}",
                newJob.Id,
                ex.Message
            );
        }
        return ApiResponse<string>.Success("Job is create");
    }

    public async Task<ApiResponse<ListJobsResponse>> ListJobsAsync(
        ListJobsRequest request,
        CancellationToken cancellationToken
    )
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Min(request.PageSize, 50);

        var query = context.Jobs.AsNoTracking();

        if (request.Status.HasValue && request.Status is not null)
            query = query.Where(j => j.Status == request.Status);

        var jobs = await query
            .OrderBy(q => q.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(s => new GetJobResponse(
                s.Id,
                s.Type,
                s.Status,
                GetJobResult(s.Result),
                s.CreatedAt,
                s.CompletedAt,
                s.RetryCount
            ))
            .ToListAsync(cancellationToken);

        var count = jobs.Count;
        var hasNext = count > pageSize;
        if (hasNext)
            jobs.RemoveAt(count - 1);

        return ApiResponse<ListJobsResponse>.Success(new ListJobsResponse(jobs, hasNext));
    }

    public async Task<ApiResponse<GetJobResponse>> GetJobByIdAsync(Guid id, CancellationToken ct)
    {
        var job = await context
            .Jobs.AsNoTracking()
            .Where(j => j.Id == id)
            .Select(s => new GetJobResponse(
                s.Id,
                s.Type,
                s.Status,
                GetJobResult(s.Result),
                s.CreatedAt,
                s.CompletedAt,
                s.RetryCount
            ))
            .FirstOrDefaultAsync(ct);

        if (job is null)
            return ApiResponse<GetJobResponse>.NotFound("Job with the id of {id} not found");

        return ApiResponse<GetJobResponse>.Success(job);
    }

    public async Task<ApiResponse<CancelJobResponse>> CancelJobAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var job = await context.Jobs.FindAsync([id], cancellationToken);
        if (job is null)
            return ApiResponse<CancelJobResponse>.Success(new CancelJobResponse());

        job.Cancel();
        await context.SaveChangesAsync(cancellationToken);

        return ApiResponse<CancelJobResponse>.Success(new CancelJobResponse());
    }

    public async Task<ApiResponse<ListJobStatistics>> ListJobStatisticsAsync(CancellationToken ct)
    {
        var jobs = await context
            .Jobs.GroupBy(k => k.Type)
            .Select(s => new Stat(s.Key, s.Count()))
            .ToListAsync(ct);

        return ApiResponse<ListJobStatistics>.Success(new ListJobStatistics(jobs));
    }

    private static JobResult? GetJobResult(string? result)
    {
        if (string.IsNullOrWhiteSpace(result))
            return null;
        try
        {
            var response = PayloadSerializer.Deserialize<JobResult>(result);
            return response;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
