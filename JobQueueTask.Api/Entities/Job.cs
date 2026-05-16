namespace JobQueueTask.Api.Entities;

public sealed class Job
{
    public Guid Id { get; private init; }
    public string Type { get; private init; } = string.Empty;
    public string Payload { get; private init; } = string.Empty;
    public JobStatus Status { get; private set; } = JobStatus.Pending;
    public string? Result { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public uint RowVersion { get; private set; }
    public int RetryCount { get; private set; } = 0;
    public int MaxRetries { get; private init; } = 3;

    private bool IsRetryExhausted => RetryCount >= MaxRetries;

    public static Job Create(string type, int maxRetries, string payload) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Type = type,
            Payload = payload,
            CreatedAt = DateTimeOffset.UtcNow,
            MaxRetries = maxRetries,
        };

    public void StartRunning()
    {
        TransitionJobStatus(JobStatus.Running);
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void Complete(string result)
    {
        TransitionJobStatus(JobStatus.Completed);
        Result = result;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string message)
    {
        TransitionJobStatus(JobStatus.Failed);
        ErrorMessage = message;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        TransitionJobStatus(JobStatus.Failed);
        ErrorMessage = "cancelled";
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void ResolveFailedJob()
    {
        if (IsRetryExhausted)
        {
            ErrorMessage = "Max retries reached.";
            if (Status != JobStatus.Failed)
            {
                TransitionJobStatus(JobStatus.Failed);
            }
            CompletedAt = DateTimeOffset.UtcNow;
            return;
        }
        IncrementRetryCount();
        TransitionJobStatus(JobStatus.Pending);
    }

    private void IncrementRetryCount() => RetryCount++;

    private void TransitionJobStatus(JobStatus newStatus)
    {
        bool isValidTransition = (Status, newStatus) switch
        {
            (JobStatus.Pending, JobStatus.Running) => true,
            (JobStatus.Running, JobStatus.Completed) => true,
            (JobStatus.Running, JobStatus.Failed) => true,
            (JobStatus.Running, JobStatus.Pending) when !IsRetryExhausted => true,
            (JobStatus.Failed, JobStatus.Pending) when !IsRetryExhausted => true,
            _ => false,
        };

        if (!isValidTransition)
        {
            throw new InvalidJobTransitionException(
                $"Cannot transition job from {Status} to {newStatus}."
            );
        }

        Status = newStatus;
    }
}

public sealed class InvalidJobTransitionException(string message) : Exception(message);
