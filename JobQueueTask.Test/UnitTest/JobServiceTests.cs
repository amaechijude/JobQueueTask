using JobQueueTask.Api.Entities;
using JobQueueTask.Api.JobHandler;
using JobQueueTask.Api.Redis;
using JobQueueTask.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JobQueueTask.Test.UnitTest;

public sealed class JobServiceTests
{
    [Fact]
    public void InvalidJobTransition_ShouldThrow()
    {
        // Arrange
        var payload = new Payload("admin@email.com", "r123");
        CreateJobRequest request = new(JobType.ExportCsv, payload);

        // Act
        var job = Job.Create(
            request.Type.ToString(),
            3,
            PayloadSerializer.Serialize(request.Payload)
        ); // jobis pending on create

        // Assert
        // cant transtion to complete from pending
        Assert.Throws<InvalidJobTransitionException>(() => job.Complete("Success"));
    }

    [Fact]
    public void RetryLogic_FailedJobWithRetriesRemaining_ShouldRequeueAsPending()
    {
        // Arrange
        var payload = new Payload("admin@email.com", "r123");
        CreateJobRequest request = new(JobType.ExportCsv, payload);

        var job = Job.Create(
            request.Type.ToString(),
            maxRetries: 3,
            PayloadSerializer.Serialize(request.Payload)
        );

        // Act - transition to Running then Failed
        job.StartRunning();
        job.MarkFailed("Processing error");

        // Assert - job is now Failed
        Assert.Equal(JobStatus.Failed, job.Status);
        Assert.Equal("Processing error", job.ErrorMessage);
        Assert.Equal(0, job.RetryCount); // No retries yet

        // Act - resolve the failed job
        job.ResolveFailedJob();

        // Assert - job should be back to Pending and retryCount incremented
        Assert.Equal(JobStatus.Pending, job.Status);
        Assert.Equal(1, job.RetryCount);
    }

    [Fact]
    public void RetryLogic_FailedJobWithExhaustedRetries_ShouldMarkFailed()
    {
        // Arrange
        var payload = new Payload("admin@email.com", "r123");
        CreateJobRequest request = new(JobType.ExportCsv, payload);

        var job = Job.Create(
            request.Type.ToString(),
            maxRetries: 3,
            PayloadSerializer.Serialize(request.Payload)
        );

        // Act - fail the job up to max retries
        for (int i = 0; i < 3; i++)
        {
            job.StartRunning();
            job.MarkFailed($"Attempt {i + 1} failed");
            job.ResolveFailedJob();
        }

        // Exhausted attempt
        job.StartRunning();
        job.MarkFailed("Fourth attempt failed");
        job.ResolveFailedJob();

        // Assert - job is permanently failed
        Assert.Equal(3, job.RetryCount);
        Assert.Equal(JobStatus.Failed, job.Status);
        Assert.Equal("Max retries reached.", job.ErrorMessage);
    }
}
