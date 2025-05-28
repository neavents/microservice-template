using System;

namespace TemporaryName.Infrastructure.Observability.Settings;

public class OtlpExporterCommonOptions
{
    public bool Enabled { get; set; } = true; // OTLP enabled by default if parent (Tracing/Metrics/Logging) is enabled
    public string? Endpoint { get; set; } // e.g., "http://localhost:4317" for gRPC, "http://localhost:4318/v1/traces" for HTTP/protobuf
    public OtlpExportProtocol Protocol { get; set; } = OtlpExportProtocol.Grpc; 
    public Dictionary<string, string> Headers { get; set; } = new(); 
    public int MaxExportBatchSize { get; set; } = 512;
    public int MaxQueueSize { get; set; } = 2048;
    public int ScheduledDelayMilliseconds { get; set; } = 5000;
    public int ExportTimeoutMilliseconds { get; set; } = 30000;
}
