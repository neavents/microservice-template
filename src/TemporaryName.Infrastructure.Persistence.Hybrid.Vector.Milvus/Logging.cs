using System;
using SharedKernel.Constants;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Vector.Milvus;

public class Logging
{
    public const int ProjectId = 16;
    public const int MilvusPersistenceBaseEventId = ProjectId * BaseLogging.IncrementPerProject;
    public const int IncrementPerClass = 1_000;
    public const int IncrementPerLog = 10;
    public const string ProjectName = "TemporaryName.Infrastructure.Persistence.Hybrid.Vector.Milvus";
}
