using Microsoft.Data.SqlClient;
using System.Data.Common;
using Tracker.SqlServer.Services;
using Tracker.SqlServer.Tests.Utils;

namespace Tracker.SqlServer.Tests.SqlIndexUsage;

[Collection("SqlServerIndexUsageStatsCollections")]
public class GetLastVersionTests : IAsyncLifetime
{
    private readonly string _connectionString;
    private readonly string _lowPrivilageConnectionString;
    private readonly DbDataSource _dataSource;
    private readonly DbDataSource _lowPrivilageDataSource;
    private readonly SqlServerIndexUsageOperations _operations;
    private readonly SqlServerIndexUsageOperations _lowPrivilagesOperations;

    private readonly string _testTableName = $"TestTable_{Guid.NewGuid():N}";

    public GetLastVersionTests()
    {
        _connectionString = TestConfiguration.GetSqlConnectionString();
        _lowPrivilageConnectionString = TestConfiguration.GetSqlLowPrivilageConnectionString();
        _dataSource = SqlClientFactory.Instance.CreateDataSource(_connectionString);
        _lowPrivilageDataSource = SqlClientFactory.Instance.CreateDataSource(_lowPrivilageConnectionString);
        _operations = new SqlServerIndexUsageOperations("test-source", _dataSource);
        _lowPrivilagesOperations = new SqlServerIndexUsageOperations("test-source-low-privilages", _lowPrivilageDataSource);
    }

    public async Task InitializeAsync()
    {
        await SqlHelpers.EnableDatabaseChangeTracking(_connectionString);
        await SqlHelpers.CreateTestTable(_connectionString, _testTableName);
    }

    public async Task DisposeAsync()
    {
        await SqlHelpers.DropTable(_connectionString, _testTableName);

        await _dataSource.DisposeAsync();
        _operations.Dispose();
    }

    [Fact]
    public async Task GetLastVersion_WhenTableHasChanges_ReturnsLatestVersion()
    {
        // Arrange
        await _operations.EnableTracking(_testTableName);

        await SqlHelpers.InsertToTestTable(_connectionString, _testTableName, 1);

        var expectedVersionQuery = $"""
            SELECT s.last_user_update
            FROM sys.dm_db_index_usage_stats s
            INNER JOIN sys.tables t ON s.object_id = t.object_id
            WHERE database_id = DB_ID() AND t.name = '{_testTableName}';
            """;
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var versionCmd = new SqlCommand(expectedVersionQuery, connection);
        var expectedVersion = (DateTime?)await versionCmd.ExecuteScalarAsync();

        // Act
        var result = await _operations.GetLastVersion(_testTableName);

        // Assert
        Assert.Equal(expectedVersion?.Ticks, result);
    }

    [Fact]
    public async Task GetLastVersion_WhenTableHasNoChanges_ReturnsZero()
    {
        // Arrange - Enable tracking but no changes
        await _operations.EnableTracking(_testTableName);

        // Act
        var result = await _operations.GetLastVersion(_testTableName);

        // Assert
        Assert.Equal(0L, result);
    }

    [Fact]
    public async Task GetLastVersion_WhenTableDoesNotExist_ThrowsException()
    {
        // Arrange
        var nonExistentTable = "NonExistentTable_" + Guid.NewGuid().ToString("N");

        // Act & Assert
        await Assert.ThrowsAsync<SqlException>(async () =>
            await _operations.GetLastVersion(nonExistentTable));
    }

    [Fact]
    public async Task GetLastVersion_WithEmptyTableName_ThrowsArgumentException()
    {
        // Arrange
        var emptyTableName = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
           await _operations.GetLastVersion(emptyTableName));
    }

    [Fact]
    public async Task GetLastVersion_WithNullTableName_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
           await _operations.GetLastVersion(null));
    }

    [Fact]
    public async Task GetLastVersion_MutlipleEnableTracking_JustRerturnsTrue()
    {
        await _operations.EnableTracking(_testTableName);
        await _operations.EnableTracking(_testTableName);
        await _operations.EnableTracking(_testTableName);
    }

    [Fact]
    public async Task GetLastVersion_WithoutKey_ReturnsDefault()
    {
        // Arrange

        // Act
        var timestamp = await _operations.GetLastVersion(CancellationToken.None);

        // Assert
        Assert.True(timestamp >= DateTimeOffset.MinValue.Ticks);
        Assert.True(timestamp <= DateTimeOffset.UtcNow.Ticks);
    }

    [Fact]
    public async Task GetLastVersion_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        await _operations.EnableTracking(_testTableName);
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await _operations.GetLastVersion(_testTableName, cts.Token));
    }

    [Fact]
    public async Task GetLastVersion_WhenUserLacksPermissions_ThrowsException()
    {
        // Arrange
        await _operations.EnableTracking(_testTableName);

        await SqlHelpers.InsertToTestTable(_connectionString, _testTableName, 1);

        // Act & Assert
        await Assert.ThrowsAsync<SqlException>(async () =>
           await _lowPrivilagesOperations.GetLastVersion(_testTableName));
    }

    [Fact]
    public async Task GetLastVersion_WithLargeNumberOfChanges_ReturnsCorrectVersion()
    {
        // Arrange
        await _operations.EnableTracking(_testTableName);

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        for (int i = 0; i < 1000; i++)
            await SqlHelpers.InsertToTestTable(_connectionString, _testTableName, i);

        // Get expected version
        var expectedVersionQuery = $"""
            SELECT s.last_user_update
            FROM sys.dm_db_index_usage_stats s
            INNER JOIN sys.tables t ON s.object_id = t.object_id
            WHERE database_id = DB_ID() AND t.name = '{_testTableName}';
            """;
        using var versionCmd = new SqlCommand(expectedVersionQuery, connection);
        var expectedVersion = (DateTime?)await versionCmd.ExecuteScalarAsync();

        // Act
        var result = await _operations.GetLastVersion(_testTableName);

        // Assert
        Assert.Equal(expectedVersion?.Ticks, result);
    }

    [Fact]
    public async Task GetLastVersion_NoChanges_VerifyISNULLHandlesNullCorrectly()
    {
        // Arrange
        await _operations.EnableTracking(_testTableName);

        // Act
        var result = await _operations.GetLastVersion(_testTableName);

        // Assert - Should return 0, not throw
        Assert.Equal(0L, result);
    }
}
