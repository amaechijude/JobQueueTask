namespace JobQueueTask.Api.JobHandler;

public class SendReportHandler : IJobHandler
{
    public async Task<string> ExecuteAsync(string payload, CancellationToken ct)
    {
        // Simulate payload deserialization
        // var request = JsonSerializer.Deserialize<JsonElement>(payload);

        // Simulate work duration
        await Task.Delay(500, ct);

        var result = new { sentAt = DateTime.UtcNow.ToString("O"), status = "delivered" };

        return PayloadSerializer.Serialize(result);
    }
}
