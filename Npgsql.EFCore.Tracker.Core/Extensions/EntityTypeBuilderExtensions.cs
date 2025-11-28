using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Npgsql.EFCore.Tracker.Core.Extensions;

public static class EntityTypeBuilderExtensions
{
    public static ModelBuilder EnableTracking<TEntity>(this ModelBuilder builder)
       where TEntity : class
    {
        var entity = builder.Entity<TEntity>();
        var schema = entity.Metadata.GetSchema() ?? string.Empty;
        var table_name = entity.Metadata.GetTableName();

        if (!string.IsNullOrEmpty(schema))
            schema += ".";

        if (string.IsNullOrEmpty(table_name))
            return builder;

        return builder.HasAnnotation("enable_tracking", $"{schema}{table_name}");
    }

    //public static EntityTypeBuilder<TEntity> EnableTracking<TEntity>(this EntityTypeBuilder<TEntity> builder)
    //   where TEntity : class
    //{
    //    var schema = builder.Metadata.GetSchema() ?? string.Empty;
    //    var table_name = builder.Metadata.GetTableName();

    //    if (!string.IsNullOrEmpty(schema))
    //        schema += ".";

    //    if (string.IsNullOrEmpty(table_name))
    //        return builder;

    //    return builder.HasAnnotation("enable_tracking", $"{schema}{table_name}");
    //}
}
