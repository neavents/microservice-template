using System;

namespace TemporaryName.Infrastructure.Observability.Settings;

public class TracingOptions
{
    public bool Enabled { get; set; } = true;
    public double SamplingProbability { get; set; } = 1.0;

    public OtlpExporterCommonOptions OtlpExporter { get; set; } = new();

    public InstrumentationOptions Instrumentations { get; set; } = new();
}
