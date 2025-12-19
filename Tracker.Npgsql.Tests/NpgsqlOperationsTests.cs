using Npgsql;
using System.Collections.Immutable;
using Tracker.Npgsql.Services;

namespace Tracker.Npgsql.Tests;

public class NpgsqlOperationsIntegrationTests : IAsyncLifetime
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=test_npgsqlops;Username=postgres;Password=postgres";
    private const string SourceId = "test-source";
    private NpgsqlDataSource _dataSource;
    private NpgsqlOperations _operations;
    private string _testTableName = "test_table_" + Guid.NewGuid().ToString("N")[..8];

    public async Task InitializeAsync()
    {
        // Create a test database or ensure the test database exists
        await CreateTestDatabaseIfNotExists();

        _dataSource = new NpgsqlDataSourceBuilder(ConnectionString).Build();
        _operations = new NpgsqlOperations(SourceId, _dataSource);

        // Create test table and required functions
        await SetupTestDatabase();
    }

    public async Task DisposeAsync()
    {
        await CleanupTestDatabase();
        _operations?.Dispose();
        _dataSource?.Dispose();
    }

    private async Task CreateTestDatabaseIfNotExists()
    {
        var masterConnectionString = ConnectionString.Replace("test_npgsqlops", "postgres");
        using var masterDataSource = new NpgsqlDataSourceBuilder(masterConnectionString).Build();

        using var checkCmd = masterDataSource.CreateCommand(
            "SELECT 1 FROM pg_database WHERE datname = 'test_npgsqlops'");

        var exists = await checkCmd.ExecuteScalarAsync();
        if (exists == null)
        {
            using var createCmd = masterDataSource.CreateCommand(
                "CREATE DATABASE test_npgsqlops");
            await createCmd.ExecuteNonQueryAsync();
        }
    }

    private async Task SetupTestDatabase()
    {
        using var connection = await _dataSource.OpenConnectionAsync();

        // Create test table
        using var createTableCmd = new NpgsqlCommand(
            $@"CREATE TABLE IF NOT EXISTS {_testTableName} (
                    id SERIAL PRIMARY KEY,
                    data TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )", connection);
        await createTableCmd.ExecuteNonQueryAsync();

        // Create the required PostgreSQL functions for tracking
        await CreateTrackingFunctions(connection);
    }

    private async Task CreateTrackingFunctions(NpgsqlConnection connection)
    {
        // These are simplified versions of the functions - you should match your actual PostgreSQL functions
        var functions = new[]
        {
                @"CREATE OR REPLACE FUNCTION enable_table_tracking(table_name TEXT)
                RETURNS BOOLEAN AS $$
                BEGIN
                    -- Simulate enabling tracking
                    RETURN true;
                END;
                $$ LANGUAGE plpgsql;",

                @"CREATE OR REPLACE FUNCTION disable_table_tracking(table_name TEXT)
                RETURNS BOOLEAN AS $$
                BEGIN
                    -- Simulate disabling tracking
                    RETURN true;
                END;
                $$ LANGUAGE plpgsql;",

                @"CREATE OR REPLACE FUNCTION is_table_tracked(table_name TEXT)
                RETURNS BOOLEAN AS $$
                BEGIN
                    -- Simulate checking if tracking is enabled
                    RETURN false;
                END;
                $$ LANGUAGE plpgsql;",

                @"CREATE OR REPLACE FUNCTION get_last_timestamp(table_name TEXT)
                RETURNS TIMESTAMP WITH TIME ZONE AS $$
                BEGIN
                    RETURN NOW();
                END;
                $$ LANGUAGE plpgsql;",

                @"CREATE OR REPLACE FUNCTION get_last_timestamps(table_names TEXT[])
                RETURNS TIMESTAMP WITH TIME ZONE[] AS $$
                DECLARE
                    result TIMESTAMP WITH TIME ZONE[];
                BEGIN
                    result := array_fill(NOW(), ARRAY[array_length(table_names, 1)]);
                    RETURN result;
                END;
                $$ LANGUAGE plpgsql;",

                @"CREATE OR REPLACE FUNCTION set_last_timestamp(table_name TEXT, ts TIMESTAMP WITH TIME ZONE)
                RETURNS BOOLEAN AS $$
                BEGIN
                    RETURN true;
                END;
                $$ LANGUAGE plpgsql;"
            };

        foreach (var function in functions)
        {
            using var cmd = new NpgsqlCommand(function, connection);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private async Task CleanupTestDatabase()
    {
        using var connection = await _dataSource.OpenConnectionAsync();

        // Drop test table
        using var dropTableCmd = new NpgsqlCommand(
            $"DROP TABLE IF EXISTS {_testTableName} CASCADE", connection);
        await dropTableCmd.ExecuteNonQueryAsync();

        // Drop functions
        var functions = new[]
        {
                "enable_table_tracking",
                "disable_table_tracking",
                "is_table_tracked",
                "get_last_timestamp",
                "get_last_timestamps",
                "set_last_timestamp"
            };

        foreach (var function in functions)
        {
            using var dropFunctionCmd = new NpgsqlCommand(
                $"DROP FUNCTION IF EXISTS {function}(TEXT)", connection);
            await dropFunctionCmd.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public void Constructor_WithDataSource_InitializesCorrectly()
    {
        // Arrange & Act
        var ops = new NpgsqlOperations("test-id", _dataSource);

        // Assert
        Assert.Equal("test-id", ops.SourceId);
    }

    [Fact]
    public void Constructor_WithConnectionString_InitializesCorrectly()
    {
        // Arrange & Act
        var ops = new NpgsqlOperations("test-id", ConnectionString);

        // Assert
        Assert.Equal("test-id", ops.SourceId);
        ops.Dispose();
    }

    [Fact]
    public void Constructor_NullSourceId_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NpgsqlOperations(null, _dataSource));
        Assert.Throws<ArgumentException>(() => new NpgsqlOperations("", _dataSource));
    }

    [Fact]
    public void Constructor_NullDataSource_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NpgsqlOperations("test-id", (NpgsqlDataSource)null));
    }

    [Fact]
    public async Task EnableTracking_ValidTable_ReturnsTrue()
    {
        // Act
        var result = await _operations.EnableTracking(_testTableName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task EnableTracking_NullKey_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _operations.EnableTracking(null));
    }

    [Fact]
    public async Task DisableTracking_ValidTable_ReturnsTrue()
    {
        // Act
        var result = await _operations.DisableTracking(_testTableName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsTracking_ValidTable_ReturnsBoolean()
    {
        // Act
        var result = await _operations.IsTracking(_testTableName);

        // Assert
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task GetLastVersion_ValidTable_ReturnsTimestamp()
    {
        // Act
        var timestamp = await _operations.GetLastVersion(_testTableName);

        // Assert
        Assert.True(timestamp > 0);
    }

    [Fact]
    public async Task GetLastVersion_InvalidTable_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidTable = "non_existent_table_" + Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var result = await _operations.GetLastVersion(invalidTable);
        });
    }

    [Fact]
    public async Task GetLastVersions_MultipleTables_ReturnsTimestamps()
    {
        // Arrange
        var tables = ImmutableArray.Create(
            _testTableName,
            _testTableName + "_2",
            _testTableName + "_3"
        );
        var versions = new long[tables.Length];

        // Create additional tables
        using var connection = await _dataSource.OpenConnectionAsync();
        for (int i = 1; i < tables.Length; i++)
        {
            using var cmd = new NpgsqlCommand(
                $"CREATE TABLE IF NOT EXISTS {tables[i]} (id SERIAL PRIMARY KEY)",
                connection);
            await cmd.ExecuteNonQueryAsync();
        }

        // Act
        await _operations.GetLastVersions(tables, versions);

        // Assert
        foreach (var version in versions)
        {
            Assert.True(version > 0);
        }

        // Cleanup
        for (int i = 1; i < tables.Length; i++)
        {
            using var cmd = new NpgsqlCommand($"DROP TABLE IF EXISTS {tables[i]}", connection);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task GetLastVersion_NoParameters_ReturnsDatabaseTimestamp()
    {
        // Act
        var timestamp = await _operations.GetLastVersion();

        // Assert
        Assert.True(timestamp > 0);
    }

    [Fact]
    public async Task SetLastVersion_ValidTable_ReturnsTrue()
    {
        // Arrange
        var testTimestamp = DateTimeOffset.UtcNow.AddHours(-1).Ticks;

        // Act
        var result = await _operations.SetLastVersion(_testTableName, testTimestamp);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task SourceId_Property_ReturnsCorrectValue()
    {
        // Assert
        Assert.Equal(SourceId, _operations.SourceId);
    }

    [Fact]
    public async Task Operations_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _operations.GetLastVersion(_testTableName, cts.Token));
    }

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        var ops = new NpgsqlOperations("dispose-test", ConnectionString);

        // Act
        ops.Dispose();
        ops.Dispose(); // Second call should not throw

        // Assert
        // No exception thrown
    }

    [Fact]
    public async Task Operations_AfterDispose_ThrowObjectDisposedException()
    {
        // Arrange
        var ops = new NpgsqlOperations("dispose-test", ConnectionString);
        ops.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await ops.GetLastVersion("test"));
    }
}