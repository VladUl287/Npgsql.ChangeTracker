using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EFCore.Tracker.Api.Demo.Database;
using Npgsql.EFCore.Tracker.AspNet.Extensions;
using Npgsql.EFCore.Tracker.Core.Migrations;
using Npgsql.EFCore.Tracker.Core.Migrations.Generators;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
{
    builder.Services.AddControllers();

    builder.Services.AddOpenApi();

    builder.Services.AddDbContext<DatabaseContext>(options =>
    {
        options
            .UseNpgsql("Host=localhost;Port=5432;Database=main;Username=postgres;Password=postgres")
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();

        options
            .ReplaceService<IMigrationsModelDiffer, CustomMigrationsModelDiffer>()
            .ReplaceService<IMigrationsSqlGenerator, CustomNpgsqlMigrationsSqlGenerator>();
    });

    builder.Services.AddTrackerSupport(Assembly.GetExecutingAssembly());
}

var app = builder.Build();
{
    app.UseTracker<DatabaseContext>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();
}
app.Run();
