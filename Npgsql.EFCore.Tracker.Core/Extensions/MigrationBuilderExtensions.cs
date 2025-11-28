using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Npgsql.EFCore.Tracker.Core.Migrations.Operations;

namespace Npgsql.EFCore.Tracker.Core.Extensions;

public static class MigrationBuilderExtensions
{
    public static OperationBuilder<EnableTrackingOperation> EnableTracking(
        this MigrationBuilder migrationBuilder,
        string table)
    {
        var operation = new EnableTrackingOperation
        {
            Table = table
        };

        migrationBuilder.Operations.Add(operation);

        return new OperationBuilder<EnableTrackingOperation>(operation);
    }
}
