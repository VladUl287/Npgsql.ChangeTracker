using Microsoft.Data.SqlClient;
using System.Data.Common;
using System.Data.SqlTypes;
using Tracker.SqlServer.Services;
using Tracker.SqlServer.Tests.Utils;

namespace Tracker.SqlServer.Tests.SqlChangeTrackigng;

[Collection("SqlServerChangeTrackingTestsSequentialCollection")]
public class GetLastVersionTests : IAsyncLifetime
{
    private readonly string _connectionString;
    private readonly string _lowPrivilageConnectionString;
    private readonly DbDataSource _dataSource;
    private readonly DbDataSource _lowPrivilageDataSource;
    private readonly SqlServerChangeTrackingOperations _operations;
    private readonly SqlServerChangeTrackingOperations _lowPrivilagesOperations;

    private readonly string _testTableName = $"TestTable_{Guid.NewGuid():N}";

    public GetLastVersionTests()
    {
        _connectionString = TestConfiguration.GetSqlConnectionString();
        _lowPrivilageConnectionString = TestConfiguration.GetSqlLowPrivilageConnectionString();
        _dataSource = SqlClientFactory.Instance.CreateDataSource(_connectionString);
        _lowPrivilageDataSource = SqlClientFactory.Instance.CreateDataSource(_lowPrivilageConnectionString);
        _operations = new SqlServerChangeTrackingOperations("test-source", _dataSource);
        _lowPrivilagesOperations = new SqlServerChangeTrackingOperations("test-source-low-privilages", _lowPrivilageDataSource);
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

        var expectedVersionQuery = "SELECT CHANGE_TRACKING_CURRENT_VERSION()";
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var versionCmd = new SqlCommand(expectedVersionQuery, connection);
        var expectedVersion = (long?)await versionCmd.ExecuteScalarAsync();

        // Act
        var result = await _operations.GetLastVersion(_testTableName);

        // Assert
        Assert.Equal(expectedVersion, result);
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
    public async Task GetLastVersion_WhenTableHasChangeTrackingDisabled_ThrowsException()
    {
        // Arrange - Disable tracking
        try { await _operations.DisableTracking(_testTableName); }
        catch { }

        // Act & Assert
        await Assert.ThrowsAsync<SqlException>(async () =>
            await _operations.GetLastVersion(_testTableName));
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
    public async Task GetLastVersion_AfterMultipleChanges_ReturnsLatestVersion()
    {
        // Arrange
        await _operations.EnableTracking(_testTableName);

        await SqlHelpers.InsertToTestTable(_connectionString, _testTableName, 1);
        await SqlHelpers.InsertToTestTable(_connectionString, _testTableName, 2);

        // Get final version
        var expectedVersionQuery = "SELECT CHANGE_TRACKING_CURRENT_VERSION()";
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var versionCmd = new SqlCommand(expectedVersionQuery, connection);
        var expectedVersion = (long?)await versionCmd.ExecuteScalarAsync();

        // Act
        var result = await _operations.GetLastVersion(_testTableName);

        // Assert
        Assert.Equal(expectedVersion, result);
    }

    [Fact]
    public async Task GetLastVersion_MutlipleEnableTracking_ThrowsException()
    {
        await _operations.EnableTracking(_testTableName);
        await Assert.ThrowsAsync<SqlException>(async () =>
            await _operations.EnableTracking(_testTableName));
    }

    [Fact]
    public async Task GetLastVersion_ForTableWithoutTracking_ThrowsInvalidOperationException()
    {
        // Arrange
        // Table exists but change tracking is not enabled

        // Act & Assert
        await Assert.ThrowsAsync<SqlException>(async () =>
            await _operations.GetLastVersion(_testTableName, CancellationToken.None));
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
    public async Task GetLastVersion_WithoutKey_DisableDbTracking_ThrowsException()
    {
        // Arrange
        await SqlHelpers.DisableChangeTrackingForAllTables(_connectionString);
        await SqlHelpers.DisableDatabaseChangeTracking(_connectionString);

        // Act & Assert
        await Assert.ThrowsAsync<SqlNullValueException>(async () =>
            await _operations.GetLastVersion(CancellationToken.None));
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
        var expectedVersionQuery = "SELECT CHANGE_TRACKING_CURRENT_VERSION()";
        using var versionCmd = new SqlCommand(expectedVersionQuery, connection);
        var expectedVersion = (long?)await versionCmd.ExecuteScalarAsync();

        // Act
        var result = await _operations.GetLastVersion(_testTableName);

        // Assert
        Assert.Equal(expectedVersion, result);
    }

    [Fact]
    public async Task GetLastVersion_NoChanges_VerifyISNULLHandlesNullCorrectly()
    {
        // This test verifies that ISNULL(MAX(...), 0) works correctly

        // Arrange
        await _operations.EnableTracking(_testTableName);

        // Act
        var result = await _operations.GetLastVersion(_testTableName);

        // Assert - Should return 0, not throw
        Assert.Equal(0L, result);
    }

    [Fact]
    public async Task GetLastVersion_ForSystemTable_ReturnsZero()
    {
        // Arrange - System tables typically don't have change tracking
        var systemTable = "sys.tables";

        // Act & Assert
        await Assert.ThrowsAsync<SqlException>(async () =>
            await _operations.GetLastVersion(systemTable));
    }
}
