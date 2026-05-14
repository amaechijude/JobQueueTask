namespace JobQueueTask.Api.JobHandler;

public sealed class ExportCsvHandler : IJobHandler
{
    public async Task<string> ExecuteAsync(string payload, CancellationToken ct)
    {
        // Simulate payload deserialization
        // var request = JsonSerializer.Deserialize<JsonElement>(payload);

        // Simulate work duration
        await Task.Delay(1000, ct);

        var result = new
        {
            fileUrl = $"https://fake-storage/exports/{Guid.NewGuid()}.csv",
            rowCount = 1500,
        };

        return PayloadSerializer.Serialize(result);
    }
}
