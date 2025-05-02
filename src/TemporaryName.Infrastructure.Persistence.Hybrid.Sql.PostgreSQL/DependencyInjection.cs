using System;
using Microsoft.Extensions.DependencyInjection;

namespace TemporaryName.Infrastructure.Persistence.Hybrid.Sql.PostgreSQL;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructurePersistenceHybridSqlPostgreSQL(this IServiceCollection services){
        
        return services;
    }
}
