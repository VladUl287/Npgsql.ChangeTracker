using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql.EFCore.Tracker.Core.Migrations.Operations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;

namespace Npgsql.EFCore.Tracker.Core.Migrations.Generators;

public sealed class CustomNpgsqlMigrationsSqlGenerator(
    MigrationsSqlGeneratorDependencies dependencies, INpgsqlSingletonOptions npgsqlSingletonOptions) :
    NpgsqlMigrationsSqlGenerator(dependencies, npgsqlSingletonOptions)
{
    protected override void Generate(MigrationOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        if (operation is EnableTrackingOperation createCustomTableOperation)
        {
            Generate(createCustomTableOperation, model, builder);
            return;
        }

        base.Generate(operation, model, builder);
    }

    private void Generate(EnableTrackingOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        builder
            .AppendLine()
            .AppendLine("// Auto-generated custom operations")
            .AppendLine();

        EndStatement(builder);
    }
}
