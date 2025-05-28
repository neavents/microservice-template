using System;
using SharedKernel.Constants;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Graph.Neo4j;

public class Logging
{
    public const int ProjectId = 30; 
    public const int Neo4jPersistenceBaseEventId = ProjectId * BaseLogging.IncrementPerProject;
    public const int IncrementPerClass = 1_000;
    public const int IncrementPerLog = 10;
    public const string ProjectName = "TemporaryName.Infrastructure.Persistence.Hybrid.Graph.Neo4j";
}
