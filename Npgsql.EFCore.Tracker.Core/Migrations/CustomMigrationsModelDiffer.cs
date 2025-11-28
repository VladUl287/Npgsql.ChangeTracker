using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Npgsql.EFCore.Tracker.Core.Migrations.Operations;

namespace Npgsql.EFCore.Tracker.Core.Migrations;

public class CustomMigrationsModelDiffer : MigrationsModelDiffer
{
    public CustomMigrationsModelDiffer(
        IRelationalTypeMappingSource typeMappingSource, 
        IMigrationsAnnotationProvider migrationsAnnotationProvider, 
        IRelationalAnnotationProvider relationalAnnotationProvider, 
        IRowIdentityMapFactory rowIdentityMapFactory, 
        CommandBatchPreparerDependencies commandBatchPreparerDependencies) : 
        base(
            typeMappingSource, 
            migrationsAnnotationProvider, 
            relationalAnnotationProvider, 
            rowIdentityMapFactory, 
            commandBatchPreparerDependencies)
    {
    }

    public override IReadOnlyList<MigrationOperation> GetDifferences(IRelationalModel source, IRelationalModel target)
    {
        var operations = base.GetDifferences(source, target);

        var customOperations = new List<MigrationOperation>
        {
            new EnableTrackingOperation
            {
                Table = "tracking",
            }
        };

        return [.. operations, .. customOperations];
    }
}
