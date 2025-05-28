using System;

namespace TemporaryName.Infrastructure.Observability.Settings;

public class LoggingOptions
{
    public bool Enabled { get; set; } = false; // Disabled by default, assume Serilog handles primary logging
    public bool IncludeFormattedMessage { get; set; } = true;
    public bool ParseStateValues { get; set; } = true;
    public OtlpExporterCommonOptions OtlpExporter { get; set; } = new();
}
