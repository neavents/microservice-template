using System;
using SharedKernel.Constants;

namespace TemporaryName.Infrastructure.Web.ExceptionHandling;

public class Logging
{
    public const int ProjectId = 20;
    public const int ExceptionHandlingBaseEventId = ProjectId * BaseLogging.IncrementPerProject;
    public const int IncrementPerClass = 1_000;
    public const int IncrementPerLog = 10;
    public const string ProjectName = "TemporaryName.Infrastructure.Web.ExceptionHandling";
}
