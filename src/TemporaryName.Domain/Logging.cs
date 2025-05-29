using System;
using SharedKernel.Constants;

namespace TemporaryName.Domain;

public class Logging
{
    public const int ProjectId = 1;
    public const int DomainBaseId = ProjectId * BaseLogging.IncrementPerProject;
    public const int IncrementPerClass = 1_000;
    public const int IncrementPerLog = 10;
    public const string ProjectName = "TemporaryName.Domain";
}
