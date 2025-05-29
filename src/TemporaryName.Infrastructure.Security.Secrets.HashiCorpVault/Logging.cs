using System;
using SharedKernel.Constants;

namespace TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault;

public class Logging
{
    public const int ProjectId = 18;
    public const int HashiCorpVaultBaseEventId = ProjectId * BaseLogging.IncrementPerProject;
    public const int IncrementPerClass = 1_000;
    public const int IncrementPerLog = 10;
    public const string ProjectName = "TemporaryName.Infrastructure.Security.Secrets.HashiCorpVault";
}
