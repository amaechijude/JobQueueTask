namespace JobQueueTask.Api.JobHandler;

public sealed class ExportCsvHandler : IJobHandler
{
    public async Task<string> ExecuteAsync(string payload, CancellationToken ct)
    {
        var request = PayloadSerializer.Deserialize<ExportCsvRequest>(payload);

        // Simulate work duration
        await Task.Delay(1000, ct);

        var result = await Dowork(ct);

        return result is null ? string.Empty : PayloadSerializer.Serialize(result);
    }

    private static async Task<ExportCsvResponse?> Dowork(CancellationToken ct)
    {
        // Simulate work duration
        await Task.Delay(1000, ct);
        int random = Random.Shared.Next(20);

        return random % 2 == 0 // simulate chances of failure
            ? new ExportCsvResponse($"https://fake-storage/exports/{Guid.NewGuid()}.csv", 1500)
            : null;
    }
}

public sealed record ExportCsvRequest(Guid DatasetId, string[] Filters);

public sealed record ExportCsvResponse(string FileUrl, int RowCount);
