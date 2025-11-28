using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations.Design;

namespace Npgsql.EFCore.Tracker.Core.Migrations;

public class MyDesignTimeServices : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection services)
        => services.AddSingleton<ICSharpMigrationOperationGenerator, MyCSharpMigrationOperationGenerator>();
}
