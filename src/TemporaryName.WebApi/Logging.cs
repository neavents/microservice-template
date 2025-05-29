using System;
using SharedKernel.Constants;

namespace TemporaryName.WebApi;

public class Logging
{
    public const int ProjectId = 0;
    public const int WebApiBaseEventId = ProjectId * BaseLogging.IncrementPerProject;
    public const int IncrementPerClass = 1_000;
    public const int IncrementPerLog = 10;
    public const string ProjectName = "TemporaryName.WebApi";
}
