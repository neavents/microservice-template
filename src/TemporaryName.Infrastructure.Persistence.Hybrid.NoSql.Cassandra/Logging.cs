using System;
using SharedKernel.Constants;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.NoSql.Cassandra;

public class Logging
{
    public const int ProjectId = 13;  
    public const int CassandraPersistenceBaseEventId = ProjectId * BaseLogging.IncrementPerProject;
    public const int IncrementPerClass = 1_000;
    public const int IncrementPerLog = 10;
    public const string ProjectName = "TemporaryName.Infrastructure.Persistence.Hybrid.NoSql.Cassandra";
}
