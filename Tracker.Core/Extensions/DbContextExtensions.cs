using Microsoft.EntityFrameworkCore;

namespace Tracker.Core.Extensions;

public static class DbContextExtensions
{
    public static IEnumerable<string> GetTablesNames<TContext>(this TContext context, Type[] entities) where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(entities, nameof(entities));

        foreach (var entity in entities)
        {
            var entityType = context.Model.FindEntityType(entity) ??
                throw new NullReferenceException($"Table entity type not found for type {entity.FullName}");

            var tableName = entityType.GetSchemaQualifiedTableName() ??
                throw new NullReferenceException($"Table entity type not found for type {entity.FullName}");

            yield return tableName;
        }
    }
}
