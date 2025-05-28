using System;

namespace TemporaryName.Infrastructure.Observability.Settings;

public class ConsoleExporterOptions
{
    public bool EnableTracing { get; set; } = false; 
    public bool EnableMetrics { get; set; } = false;
    public bool EnableLogging { get; set; } = false;
}
