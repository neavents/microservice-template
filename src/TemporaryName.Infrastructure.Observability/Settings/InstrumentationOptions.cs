using System;

namespace TemporaryName.Infrastructure.Observability.Settings;

public class InstrumentationOptions
{
    public bool AspNetCore { get; set; } = true;
    public bool HttpClient { get; set; } = true;
    public bool EntityFrameworkCore { get; set; } = true;
    public bool MassTransit { get; set; } = true;
    public bool GrpcNetClient { get; set; } = false;
    public bool Runtime { get; set; } = true;
    public bool Process { get; set; } = true;
}
