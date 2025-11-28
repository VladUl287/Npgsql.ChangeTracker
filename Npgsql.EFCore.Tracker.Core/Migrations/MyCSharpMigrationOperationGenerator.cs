using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql.EFCore.Tracker.Core.Migrations.Operations;

namespace Npgsql.EFCore.Tracker.Core.Migrations
{
    public class MyCSharpMigrationOperationGenerator : CSharpMigrationOperationGenerator
    {
        public MyCSharpMigrationOperationGenerator(CSharpMigrationOperationGeneratorDependencies dependencies) : base(dependencies)
        {
        }

        protected override void Generate(MigrationOperation operation, IndentedStringBuilder builder)
        {
            if (operation is EnableTrackingOperation)
            {
                builder.AppendLine("// migrationBuilder.Operations.Add(operation);");
            }
            else
            {
                base.Generate(operation, builder);
            }
        }
    }
}
