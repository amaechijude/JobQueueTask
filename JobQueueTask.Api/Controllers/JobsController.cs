using JobQueueTask.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobQueueTask.Api.Controllers;

[ApiController]
[Route("jobs")]
public sealed class JobsController(IJobService jobService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateJob(
        [FromBody] CreateJobRequest request,
        CancellationToken cancellationToken
    )
    {
        var response = await jobService.CreateJobAsync(request, cancellationToken);
        return response.IsSuccess ? Ok(response.Data) : BadRequest(response.Error);
    }

    [HttpGet("{jobId:guid}")]
    public async Task<IActionResult> GetJobById([FromRoute] Guid jobId, CancellationToken ct)
    {
        var response = await jobService.GetJobByIdAsync(jobId, ct);
        return response.IsSuccess ? Ok(response.Data) : BadRequest(response.Error);
    }

    [HttpGet]
    public async Task<IActionResult> ListJobs(
        [FromQuery] ListJobsRequest request,
        CancellationToken cancellationToken
    )
    {
        var response = await jobService.ListJobsAsync(request, cancellationToken);
        return response.IsSuccess ? Ok(response.Data) : BadRequest(response.Error);
    }

    [HttpPost("{jobId:guid}/cancel")]
    public async Task<IActionResult> CancelJob(
        [FromRoute] Guid jobId,
        CancellationToken cancellationToken
    )
    {
        var response = await jobService.CancelJobAsync(jobId, cancellationToken);
        return response.IsSuccess ? Ok(response.Data) : BadRequest(response.Error);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> ListJobstats(CancellationToken cancellationToken)
    {
        var response = await jobService.ListJobStatisticsAsync(cancellationToken);
        return response.IsSuccess ? Ok(response.Data) : BadRequest(response.Error);
    }
}
