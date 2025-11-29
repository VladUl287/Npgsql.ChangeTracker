using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Tracker.Api.Demo.Database;
using Tracker.AspNet.Extensions;

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

    app.UseAuthorization();

    app.UseTracker<DatabaseContext>();

    app.MapGet("/api/users", () => "Get all users")
        .WithTracking("roles");

    app.MapControllers();
}
app.Run();
