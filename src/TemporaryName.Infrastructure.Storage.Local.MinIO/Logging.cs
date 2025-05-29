using System;
using SharedKernel.Constants;

namespace TemporaryName.Infrastructure.Storage.Local.MinIO;

public class Logging
{
    public const int ProjectId = 19;
    public const int StorageMinIOBaseEventId = ProjectId * BaseLogging.IncrementPerProject;
    public const int IncrementPerClass = 1_000;
    public const int IncrementPerLog = 10;
    public const string ProjectName = "TemporaryName.Infrastructure.Storage.Local.MinIO";
}
