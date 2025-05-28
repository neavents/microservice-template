using System;

namespace TemporaryName.Infrastructure.Observability.Settings;

public class MetricsOptions
{
    public bool Enabled { get; set; } = true;
    public int ExportIntervalMilliseconds { get; set; } = 60000;
    public OtlpExporterCommonOptions OtlpExporter { get; set; } = new();
    public InstrumentationOptions Instrumentations { get; set; } = new();
}
