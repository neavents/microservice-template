namespace TemporaryName.Tools.Persistence.Migrations.Abstractions;

/// <summary>
/// Factory to retrieve the appropriate migration runner based on database type.
/// </summary>
public interface IMigrationServiceFactory
{
    IMigrationRunner GetRunner(string dbType);
}