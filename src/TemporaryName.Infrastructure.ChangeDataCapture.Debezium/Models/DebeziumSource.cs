using System;
using System.Text.Json.Serialization;

namespace TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Models;

public class DebeziumSource
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("connector")]
    public string? Connector { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; } // database.server.name

    [JsonPropertyName("ts_ms")]
    public long TsMs { get; set; } // Timestamp of change in source DB

    [JsonPropertyName("snapshot")]
    public string? Snapshot { get; set; } // "true", "false", "last", "incremental"

    [JsonPropertyName("db")]
    public string? Db { get; set; }

    [JsonPropertyName("sequence")]
    public string? Sequence { get; set; } // For SQL Server: "[\"NULL\",\"<LSN_HEX_STRING>\"]", PG: LSN string

    [JsonPropertyName("schema")]
    public string? SchemaName { get; set; } // Renamed for clarity from 'schema'

    [JsonPropertyName("table")]
    public string? TableName { get; set; } // Renamed for clarity

    [JsonPropertyName("txId")]
    public long? TxId { get; set; }

    [JsonPropertyName("lsn")]
    public decimal? Lsn { get; set; } // PostgreSQL specific, can be ulong

    [JsonPropertyName("xmin")]
    public long? Xmin { get; set; } // PostgreSQL specific
}
