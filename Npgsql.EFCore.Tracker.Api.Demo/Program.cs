using Microsoft.EntityFrameworkCore;
using Npgsql.EFCore.Tracker.Api.Demo.Database;
using Npgsql.EFCore.Tracker.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);
{
    builder.Services.AddControllers();

    builder.Services.AddDbContext<DatabaseContext>(options =>
    {
        options.UseNpgsql(
            "Host=localhost;Port=5432;Database=HubDb;Username=postgres;Password=qwerty",
            npgsql =>
            {
                npgsql.EnableTableTrackingSupport();
            });
    });

    builder.Services.AddOpenApi();

}

var app = builder.Build();
{
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();
}
app.Run();
