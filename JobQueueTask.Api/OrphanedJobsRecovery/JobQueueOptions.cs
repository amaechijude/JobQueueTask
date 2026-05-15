using System.ComponentModel.DataAnnotations;

namespace JobQueue.Api;

public sealed class JobQueueOptions
{
    public const string Key = "JobQueue";

    [Range(1, 5)]
    public int WorkerCount { get; set; }

    [Range(1, 5)]
    public int OrphanTimeoutMinutes { get; set; }

    [Range(1, 60)]
    public int RecoveryIntervalSeconds { get; set; }
}
