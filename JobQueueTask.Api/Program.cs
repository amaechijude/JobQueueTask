using JobQueueTask.Api;
using JobQueueTask.Api.Entities;
using JobQueueTask.Api.JobHandler;
using JobQueueTask.Api.OrphanedJobsRecovery;
using JobQueueTask.Api.Redis;
using JobQueueTask.Api.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<JobDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql =>
            npgsql.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null
            )
    )
);

var redis = builder.Configuration.GetConnectionString("redis") ?? "localhost";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redis)); // injected through aspire

builder.Services.AddSingleton<IJobQueue, RedisJobQueue>();
builder.Services.AddScoped<IJobService, JobService>();

// handlers
builder.Services.AddKeyedScoped<IJobHandler, ExportCsvHandler>(JobType.ExportCsv.ToString());
builder.Services.AddKeyedScoped<IJobHandler, SendReportHandler>(JobType.SendReport.ToString());

builder.Services.AddHostedService<JobProcessingWorker>();
builder.Services.AddHostedService<OrphanRecovery>();

builder
    .Services.Configure<JobQueueOptions>(builder.Configuration.GetSection(JobQueueOptions.Key))
    .AddOptions<JobQueueOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();

//
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CustomExceptionHandler>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    // Apply migrations
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<JobDbContext>();
        // Wait for the database to be ready and apply migrations
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapPost(
    "/pend",
    async (JobDbContext context, IJobQueue jobQueue, CancellationToken ct) =>
    {
        var ids = await context
            .Jobs.AsNoTracking()
            .Where(j =>
                j.Status == JobStatus.Pending && j.CreatedAt < DateTimeOffset.UtcNow.AddMinutes(-10)
            )
            .OrderBy(j => j.Id)
            .Take(50)
            .Select(s => s.Id)
            .ToListAsync(ct);

        await jobQueue.EnqueueAsync(ids, ct);
        return Results.Ok(new { Messsage = "Enqueued", Number = ids.Count });
    }
);
app.Run();
;
