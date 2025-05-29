using System;
using SharedKernel.Constants;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Sql.PostgreSQL;

public class Logging
{
    public const int ProjectId = 15;
    public const int PostgreSqlPersistenceBaseEventId = ProjectId * BaseLogging.IncrementPerProject;
    public const int IncrementPerClass = 1_000;
    public const int IncrementPerLog = 10;
    public const string ProjectName = "TemporaryName.Infrastructure.Persistence.Hybrid.Sql.PostgreSQL";
}
