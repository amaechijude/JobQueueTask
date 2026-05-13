using System.Text.Json.Serialization;

namespace JobQueueTask.Api.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum JobStatus
{
    Pending,
    Running,
    Completed,
    Failed,
}
