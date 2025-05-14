using System.Text.Json.Serialization;
using TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Models;

public class DebeziumPayload<TData>
{
    [JsonPropertyName("before")]
    public TData? Before { get; set; }

    [JsonPropertyName("after")]
    public TData? After { get; set; }

    [JsonPropertyName("source")]
    public DebeziumSource? Source { get; set; }

    [JsonPropertyName("op")]
    public string? Op { get; set; } // c, u, d, r, t (truncate)

    [JsonPropertyName("ts_ms")]
    public long? TsMs { get; set; } // Timestamp of event processed by Debezium

    [JsonPropertyName("transaction")]
    public DebeziumTransactionMetadata? Transaction { get; set; }
}