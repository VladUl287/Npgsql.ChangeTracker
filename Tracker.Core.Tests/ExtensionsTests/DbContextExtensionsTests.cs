using Microsoft.EntityFrameworkCore;
using Tracker.Core.Services;

namespace Tracker.Core.Tests.ExtensionsTests;

public class DbContextExtensionsTests
{
    private static readonly DefaultTableNameResolver defaultTableNameResolver = new DefaultTableNameResolver();

    [Fact]
    public void Null_Context()
    {
        // Arrange
        Type[] types = [typeof(Role)];
        EmptyTestDbContext dbContext = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>("context", () =>
        {
            defaultTableNameResolver.GetTablesNames(dbContext, types).ToArray();
        });
    }

    [Fact]
    public void Null_Types_Array()
    {
        // Arrange
        Type[] types = null;
        var dbContext = new EmptyTestDbContext(_dbContextOptions);

        // Act & Assert
        Assert.Throws<ArgumentNullException>("entities", () =>
        {
            defaultTableNameResolver.GetTablesNames(dbContext, types).ToArray();
        });
    }

    [Fact]
    public void Empty_Context()
    {
        // Arrange
        Type[] types = [typeof(Role)];
        var dbContext = new EmptyTestDbContext(_dbContextOptions);

        // Act & Assert
        Assert.Throws<NullReferenceException>(() =>
        {
            defaultTableNameResolver.GetTablesNames(dbContext, types).ToArray();
        });
    }

    [Fact]
    public void Not_Presented_Entity_Type()
    {
        // Arrange
        Type[] types = [typeof(User)];
        var dbContext = new TestDbContext(_dbContextOptions);

        // Act & Assert
        Assert.Throws<NullReferenceException>(() =>
        {
            defaultTableNameResolver.GetTablesNames(dbContext, types).ToArray();
        });
    }

    [Fact]
    public void Presented_Entity_Type()
    {
        // Arrange
        Type[] types = [typeof(Role)];
        var dbContext = new TestDbContext(_dbContextOptions);

        //Act
        var tables = defaultTableNameResolver.GetTablesNames(dbContext, types);

        // Assert
        Assert.Equal(tables, ["Roles"]);
    }

    [Fact]
    public void Configured_Entity_Type()
    {
        // Arrange
        Type[] types = [typeof(Role)];
        var dbContext = new ConfiguredTestDbContext(_dbContextOptions);

        //Act
        var tables = defaultTableNameResolver.GetTablesNames(dbContext, types);

        // Assert
        Assert.Equal(tables, ["roles"]);
    }

    [Fact]
    public void Multiple_Entity_Types()
    {
        // Arrange
        Type[] types = [typeof(Role), typeof(User)];
        var dbContext = new ConfiguredTestDbContext(_dbContextOptions);

        //Act
        var tables = defaultTableNameResolver.GetTablesNames(dbContext, types).ToArray();

        // Assert
        Assert.Equal(tables, new string[] { "roles", "Users" });
    }

    private static readonly DbContextOptions _dbContextOptions = new DbContextOptionsBuilder()
        .UseNpgsql("Host=localhost;Port=5432;Database=main;Username=postgres;Password=postgres")
        .Options;

    private sealed class Role
    {
        public int Id { get; init; }
    }
    private sealed class User
    {
        public int Id { get; init; }
    }

    private sealed class EmptyTestDbContext(DbContextOptions options) : DbContext(options) { }

    private sealed class TestDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Role> Roles { get; init; }
    }

    private sealed class ConfiguredTestDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Role> Roles { get; init; }
        public DbSet<User> Users { get; init; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>(builder =>
            {
                builder.ToTable("roles");
            });
        }
    }
}
