using Microsoft.EntityFrameworkCore;
using Npgsql.EFCore.Tracker.Api.Demo.Database.Entities;
using Npgsql.EFCore.Tracker.Core.Extensions;

namespace Npgsql.EFCore.Tracker.Api.Demo.Database;

public sealed class DatabaseContext : DbContext
{
    public DbSet<Role> Roles => Set<Role>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.EnableTracking();
        });
    }
}
