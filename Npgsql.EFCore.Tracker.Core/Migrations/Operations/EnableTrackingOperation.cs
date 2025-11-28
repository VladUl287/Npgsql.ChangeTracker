using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Npgsql.EFCore.Tracker.Core.Migrations.Operations;

public sealed class EnableTrackingOperation : MigrationOperation
{
    public required string Table { get; init; }
}
