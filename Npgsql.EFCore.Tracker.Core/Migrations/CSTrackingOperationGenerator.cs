using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql.EFCore.Tracker.Core.Migrations.Operations;

namespace Npgsql.EFCore.Tracker.Core.Migrations;

public sealed class CSTrackingOperationGenerator(CSharpMigrationOperationGeneratorDependencies dependencies)
    : CSharpMigrationOperationGenerator(dependencies)
{
    protected override void Generate(MigrationOperation operation, IndentedStringBuilder builder)
    {
        if (operation is EnableTrackingOperation trackOperation)
        {
            builder.AppendLine($".EnableTracking(\"{trackOperation.Table}\");");
            return;
        }

        base.Generate(operation, builder);
    }
}
