using System;
using System.Text.Json.Serialization;

namespace TemporaryName.Infrastructure.ChangeDataCapture.Debezium.Models;

public class DebeziumTransactionMetadata
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("total_order")]
    public long? TotalOrder { get; set; }

    [JsonPropertyName("data_collection_order")]
    public long? DataCollectionOrder { get; set; }
}
