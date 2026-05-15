using System.Runtime.CompilerServices;

namespace JobQueueTask.Api.JobHandler;

public class SendReportHandler : IJobHandler
{
    public async Task<string> ExecuteAsync(string payload, CancellationToken ct)
    {
        // Simulate payload deserialization
        var request = PayloadSerializer.Deserialize<SendReportRequest>(payload);

        var result = await Dowork(ct);

        return result is null ? string.Empty : PayloadSerializer.Serialize(result);
    }

    private static async Task<SendReportResponse?> Dowork(CancellationToken ct)
    {
        // Simulate work duration
        await Task.Delay(1000, ct);
        int random = Random.Shared.Next(20);

        return random % 2 == 0 // simulate chances of failure
            ? new SendReportResponse(DateTimeOffset.UtcNow.ToString("O"), "delivered")
            : null;
    }
}

public sealed record SendReportRequest(string ReciepientEmail, string ReportId);

public sealed record SendReportResponse(string SentAt, string Status);
