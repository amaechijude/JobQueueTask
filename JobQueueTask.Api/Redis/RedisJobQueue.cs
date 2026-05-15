using StackExchange.Redis;

namespace JobQueueTask.Api.Redis;

public sealed class RedisJobQueue(IConnectionMultiplexer connectionMultiplexer) : IJobQueue
{
    private const string QueueKey = "jobs:queue";

    private readonly IDatabase _redisdb = connectionMultiplexer.GetDatabase();

    public async Task EnqueueAsync(Guid jobId, CancellationToken cancellationToken)
    {
        await RedisRetryStrategy.pipeline.ExecuteAsync(
            async token => await _redisdb.ListRightPushAsync(QueueKey, jobId.ToString()),
            cancellationToken
        );
    }

    public async Task EnqueueAsync(IEnumerable<Guid> jobIds, CancellationToken cancellationToken)
    {
        var redisValues = jobIds.Select(j => new RedisValue(j.ToString())).ToArray();

        if (redisValues.Length == 0)
            return;

        await RedisRetryStrategy.pipeline.ExecuteAsync(
            async token => await _redisdb.ListRightPushAsync(QueueKey, redisValues),
            cancellationToken
        );
    }

    public async Task<Guid?> DequeueAsync(CancellationToken cancellationToken)
    {
        RedisValue res = await RedisRetryStrategy.pipeline.ExecuteAsync(
            async token => await _redisdb.ListLeftPopAsync(QueueKey),
            cancellationToken
        );

        if (res.IsNullOrEmpty)
            return null;

        if (!Guid.TryParse((string?)res, out var jobId))
            return null;

        return jobId;
    }
}
