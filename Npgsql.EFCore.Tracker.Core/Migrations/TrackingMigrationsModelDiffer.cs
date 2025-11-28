using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Npgsql.EFCore.Tracker.Core.Migrations.Operations;

namespace Npgsql.EFCore.Tracker.Core.Migrations;

public sealed class TrackingMigrationsModelDiffer : MigrationsModelDiffer
{
    public TrackingMigrationsModelDiffer(
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

        var customOperations = new List<MigrationOperation>();

        var targetAnnotaions = target?.Model.GetAnnotations() ?? [];
        var sourceAnnotations = source?.Model.GetAnnotations() ?? [];

        var addedAnnotations = new List<IAnnotation>();
        foreach (var targetAnnon in targetAnnotaions)
        {
            if (sourceAnnotations.Any(c => c.Name == targetAnnon.Name && c.Value == targetAnnon.Value))
                continue;

            if (targetAnnon.Name != "enable_tracking" || targetAnnon.Value is null)
                continue;

            addedAnnotations.Add(targetAnnon);
        }

        foreach (var anno in addedAnnotations)
        {
            var valueStr = anno.Value?.ToString();
            if (string.IsNullOrEmpty(valueStr))
                continue;

            customOperations.Add(new EnableTrackingOperation
            {
                Table = valueStr,
            });

            Console.WriteLine(valueStr);
        }

        return [.. operations, .. customOperations];
    }
}
