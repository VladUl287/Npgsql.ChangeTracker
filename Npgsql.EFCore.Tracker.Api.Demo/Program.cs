using Microsoft.EntityFrameworkCore;
using Npgsql.EFCore.Tracker.Api.Demo.Database;
using Npgsql.EFCore.Tracker.AspNet.Extensions;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
{
    builder.Services.AddControllers();

    builder.Services.AddOpenApi();

    builder.Services
        .AddTracker<DatabaseContext>(Assembly.GetExecutingAssembly())
        .AddDbContext<DatabaseContext>(options =>
        {
            options
                .UseNpgsql("Host=localhost;Port=5432;Database=main;Username=postgres;Password=postgres")
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors();
        });
}

var app = builder.Build();
{
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.UseTracker<DatabaseContext>();

    app.MapControllers();
}
app.Run();
