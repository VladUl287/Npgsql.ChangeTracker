using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Npgsql.EFCore.Tracker.Core.Migrations.Generators;

namespace Npgsql.EFCore.Tracker.Core.Migrations;

public class MyDesignTimeServices : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection services)
    {
        services.AddSingleton<IMigrationsCodeGenerator, CustomCSharpMigrationsGenerator>();
        services.AddSingleton<ICSharpMigrationOperationGenerator, CSTrackingOperationGenerator>();
    }
}
