using System;
using SharedKernel.Constants;

namespace TemporaryName.Infrastructure.ChangeDataCapture.Debezium;

public class Logging
{
    public const int ProjectId = 6;
    public const int InfrastructureDebeziumnBaseId = ProjectId * BaseLogging.IncrementPerProject;
    public const int IncrementPerClass = 1_000;
    public const int IncrementPerLog = 10;
    public const string ProjectName = "TemporaryName.Infrastructure.ChangeDataCapture.Debezium";
}
