using Microsoft.Extensions.DependencyInjection;
using TemporaryName.Tools.Persistence.Migrations.Abstractions;

namespace TemporaryName.Tools.Persistence.Migrations.Implementations;

public class MigrationServiceFactory(IServiceProvider serviceProvider) : IMigrationServiceFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public IMigrationRunner GetRunner(string dbType)
    {
        if (string.IsNullOrWhiteSpace(dbType))
        {
            throw new ArgumentException("Database type cannot be null or empty.", nameof(dbType));
        }

        // Use keyed services to get the appropriate runner
        IMigrationRunner? runner = _serviceProvider.GetKeyedService<IMigrationRunner>(dbType.ToLowerInvariant());

        return runner ?? throw new NotSupportedException($"Database type '{dbType}' is not supported for migrations.");
    }
}