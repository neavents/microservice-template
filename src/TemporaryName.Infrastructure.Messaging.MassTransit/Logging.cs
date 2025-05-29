using System;
using SharedKernel.Constants;

namespace TemporaryName.Infrastructure.Messaging.MassTransit;

public class Logging
{
    public const int ProjectId = 8;
    public const int MassTransitBaseId = ProjectId * BaseLogging.IncrementPerProject;
    public const int IncrementPerClass = 1_000;
    public const int IncrementPerLog = 10;
    public const string ProjectName = "TemporaryName.Infrastructure.Messaging.MassTransit";
    
}
