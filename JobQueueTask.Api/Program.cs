using JobQueueTask.Api.Entities;
using JobQueueTask.Api.JobHandler;
using JobQueueTask.Api.Redis;
using JobQueueTask.Api.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<JobDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

var redis = builder.Configuration.GetConnectionString("Redis") ?? string.Empty;
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redis));

builder.Services.AddSingleton<IJobHandler, JobHandler>();
builder.Services.AddSingleton<IJobQueue, RedisJobQueue>();
builder.Services.AddScoped<IJobService, JobService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
