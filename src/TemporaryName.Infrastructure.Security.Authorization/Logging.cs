using System;
using SharedKernel.Constants;

namespace TemporaryName.Infrastructure.Security.Authorization;

public class Logging
{
    public const int ProjectId = 17;
    public const int SecurityAuthorizationBaseEventId = ProjectId * BaseLogging.IncrementPerProject;
    public const int IncrementPerClass = 1_000;
    public const int IncrementPerLog = 10;
    public const string ProjectName = "TemporaryName.Infrastructure.Security.Authorization";
}
