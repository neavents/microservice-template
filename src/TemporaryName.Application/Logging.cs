using System;
using SharedKernel.Constants;

namespace TemporaryName.Application;

public class Logging
{
    public const int ProjectId = 2;
    public const int ApplicationBaseId = ProjectId * BaseLogging.IncrementPerProject;
    public const int IncrementPerClass = 1_000;
    public const int IncrementPerLog = 10;
    public const string ProjectName = "TemporaryName.Application";
}
