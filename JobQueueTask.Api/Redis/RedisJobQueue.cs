using StackExchange.Redis;

namespace JobQueueTask.Api.Redis;

public sealed class RedisJobQueue(IConnectionMultiplexer connectionMultiplexer) : IJobQueue
{
    private const string QueueKey = "jobs:queue";

    private readonly IDatabase _redisdb = connectionMultiplexer.GetDatabase();

    public async Task EnqueueAsync(Guid jobId, CancellationToken cancellationToken)
    {
        await _redisdb.ListRightPushAsync(QueueKey, jobId.ToString());
    }

    public async Task<Guid?> DequeueAsync(CancellationToken cancellationToken)
    {
        var result = await _redisdb.ListLeftPopAsync(QueueKey);

        if (result.IsNullOrEmpty)
            return null;

        if (!Guid.TryParse((string?)result, out var jobId))
            return null;

        return jobId;
    }
}
