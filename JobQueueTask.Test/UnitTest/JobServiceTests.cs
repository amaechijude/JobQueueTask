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
    private readonly IJobService _sut;
    private readonly JobDbContext _dbContext;
    private readonly IJobQueue jobQueue;

    public JobServiceTests()
    {
        var logger = Substitute.For<ILogger<JobService>>();

        jobQueue = Substitute.For<IJobQueue>();

        var dbOptions = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new JobDbContext(dbOptions);

        _sut = new JobService(_dbContext, jobQueue, logger);
    }

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
}
