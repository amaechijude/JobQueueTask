using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using StackExchange.Redis;

namespace JobQueueTask.Api.Redis;

public static class RedisRetryStrategy
{
    public static ResiliencePipeline GetPipeline(int maxRetries) =>
        new ResiliencePipelineBuilder()
            .AddRetry(
                new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder()
                        .Handle<TimeoutException>()
                        .Handle<RedisConnectionException>()
                        .Handle<RedisTimeoutException>(),

                    MaxRetryAttempts = maxRetries,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromSeconds(2),
                    UseJitter = true,
                }
            )
            .Build();
}
