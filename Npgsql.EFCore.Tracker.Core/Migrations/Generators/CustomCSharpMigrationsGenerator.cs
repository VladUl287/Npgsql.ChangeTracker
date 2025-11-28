using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql.EFCore.Tracker.Core.Migrations.Operations;

namespace Npgsql.EFCore.Tracker.Core.Migrations.Generators;

public sealed class CustomCSharpMigrationsGenerator : CSharpMigrationsGenerator
{
    public CustomCSharpMigrationsGenerator(
        MigrationsCodeGeneratorDependencies dependencies,
        CSharpMigrationsGeneratorDependencies csharpDependencies) : base(dependencies, csharpDependencies)
    {
    }

    protected override IEnumerable<string> GetNamespaces(IEnumerable<MigrationOperation> operations)
    {
        var namespaces = base.GetNamespaces(operations).ToList();

        if (operations.Any(op => op is EnableTrackingOperation))
        {
            namespaces.Add("Npgsql.EFCore.Tracker.Core.Extensions");
        }

        return [.. namespaces];
    }
}
