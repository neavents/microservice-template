using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace TemporaryName.Infrastructure.Persistence.Common.EFCore.Extensions;

public static class DbContextExtensions
{

    /// <summary>
    /// Example: A common convention to map entity and property names to snake_case for relational databases.
    /// This is database-agnostic in its definition but results in provider-specific table/column names.
    /// </summary>
    public static void ConfigureSnakeCaseNaming(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Replace table names
            entity.SetTableName(ToSnakeCase(entity.GetTableName() ?? entity.ClrType.Name));

            // Replace column names
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.GetColumnName(StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema())) ?? property.Name));
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName() ?? $"PK_{entity.ClrType.Name}"));
            }

            foreach (var key in entity.GetForeignKeys())
            {
                key.SetConstraintName(ToSnakeCase(key.GetConstraintName() ?? $"FK_{entity.ClrType.Name}_{key.PrincipalEntityType.ClrType.Name}"));
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName() ?? $"IX_{entity.ClrType.Name}_{string.Join("_", index.Properties.Select(p => p.Name))}"));
            }
        }
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) { return input; }

        var startUnderscores = System.Text.RegularExpressions.Regex.Match(input, @"^_+");
        return startUnderscores + System.Text.RegularExpressions.Regex.Replace(input[startUnderscores.Length..], @"([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();
    }
}
