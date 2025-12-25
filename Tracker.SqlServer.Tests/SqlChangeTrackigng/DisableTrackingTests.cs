using Microsoft.Data.SqlClient;
using System.Data.Common;
using Tracker.SqlServer.Services;
using Tracker.SqlServer.Tests.Utils;

namespace Tracker.SqlServer.Tests.SqlChangeTrackigng;

[Collection("SqlServerChangeTrackingCollection")]
public class DisableTrackingTests : IAsyncLifetime
{
    private readonly string connectionString;
    private readonly string _lowPrivilageConnectionString;
    private readonly DbDataSource _dataSource;
    private readonly DbDataSource _lowPrivilageDataSource;
    private readonly SqlServerChangeTrackingOperations _operations;
    private readonly SqlServerChangeTrackingOperations _lowPrivilagesOperations;

    private readonly string _testTableName = $"TestTable_{Guid.NewGuid():N}";
    private readonly string _testTableName2 = $"TestTable_{Guid.NewGuid():N}";

    public DisableTrackingTests()
    {
        connectionString = TestConfiguration.GetSqlConnectionString();
        _lowPrivilageConnectionString = TestConfiguration.GetSqlLowPrivilageConnectionString();
        _dataSource = SqlClientFactory.Instance.CreateDataSource(connectionString);
        _lowPrivilageDataSource = SqlClientFactory.Instance.CreateDataSource(_lowPrivilageConnectionString);
        _operations = new SqlServerChangeTrackingOperations("test-source", _dataSource);
        _lowPrivilagesOperations = new SqlServerChangeTrackingOperations("test-source-low-privilages", _lowPrivilageDataSource);
    }

    public async Task InitializeAsync()
    {
        await SqlHelpers.EnableDatabaseChangeTracking(connectionString);
        await SqlHelpers.CreateTestTable(connectionString, _testTableName);
        await SqlHelpers.CreateTestTable(connectionString, _testTableName2);
    }

    public async Task DisposeAsync()
    {
        await SqlHelpers.DropTable(connectionString, _testTableName);
        await SqlHelpers.DropTable(connectionString, _testTableName2);
        await SqlHelpers.DisableChangeTrackingForAllTables(connectionString);

        await _dataSource.DisposeAsync();
        _operations.Dispose();
    }

    [Fact]
    public async Task DisableTracking_ForEnabledTable_DisablesSuccessfully()
    {
        // Arrange
        await _operations.EnableTracking(_testTableName, CancellationToken.None);

        // Act
        var result = await _operations.DisableTracking(_testTableName, CancellationToken.None);

        // Assert
        Assert.True(result);

        // Verify tracking is disabled
        var isTracking = await _operations.IsTracking(_testTableName, CancellationToken.None);
        Assert.False(isTracking);
    }

    [Fact]
    public async Task DisableTracking_WhenTableExistsAndChangeTrackingEnabled_ReturnsTrue()
    {
        // Arrange
        await _operations.EnableTracking(_testTableName);

        // Act
        var result = await _operations.DisableTracking(_testTableName);

        // Assert
        Assert.True(result);
        Assert.False(await _operations.IsTracking(_testTableName));
    }

    [Fact]
    public async Task DisableTracking_WhenTableExistsButChangeTrackingNotEnabled_ThrowsException()
    {
        // Arrange - Ensure change tracking is NOT enabled for the table
        try
        {
            await _operations.DisableTracking(_testTableName);
        }
        catch
        { }

        // Act
        // Assert
        await Assert.ThrowsAsync<SqlException>(async () => await _operations.DisableTracking(_testTableName));
    }

    [Fact]
    public async Task DisableTracking_WhenTableDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var nonExistentTable = "NonExistentTable_" + Guid.NewGuid().ToString("N");

        // Act & Assert
        await Assert.ThrowsAsync<SqlException>(async () => await _operations.DisableTracking(nonExistentTable));
    }

    [Fact]
    public async Task DisableTracking_WithEmptyTableName_ThrowsArgumentException()
    {
        // Arrange
        var emptyTableName = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _operations.DisableTracking(emptyTableName));
    }

    [Fact]
    public async Task DisableTracking_WithNullTableName_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _operations.DisableTracking(null));
    }

    [Fact]
    public async Task DisableTracking_WhenTableHasSchemaPrefix_WorksCorrectly()
    {
        // Arrange
        var schemaTableName = "dbo." + _testTableName2;
        await _operations.EnableTracking(_testTableName2);

        // Act
        var result = await _operations.DisableTracking(schemaTableName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DisableTracking_WhenCalledAfterTableDropped_ThrowsException()
    {
        // Act & Assert - Should throw because table doesn't have change tracking
        await Assert.ThrowsAsync<SqlException>(async () =>
            await _operations.DisableTracking(_testTableName));
    }

    [Fact]
    public async Task DisableTracking_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        await _operations.EnableTracking(_testTableName);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await _operations.DisableTracking(_testTableName, cts.Token));
    }

    [Fact]
    public async Task DisableTracking_WhenUserLacksAlterPermission_ReturnFalse()
    {
        await _operations.EnableTracking(_testTableName);

        await Assert.ThrowsAsync<SqlException>(async () =>
            await _lowPrivilagesOperations.DisableTracking(_testTableName));
    }

    [Fact]
    public async Task DisableTracking_VerifyNoSideEffectsOnOtherTables()
    {
        // Arrange
        await _operations.EnableTracking(_testTableName);
        await _operations.EnableTracking(_testTableName2);

        // Act - Disable tracking on first table only
        var result = await _operations.DisableTracking(_testTableName);

        // Assert
        Assert.True(result);
        Assert.False(await _operations.IsTracking(_testTableName));
        Assert.True(await _operations.IsTracking(_testTableName2));
    }

    [Fact]
    public async Task DisableTracking_ForAlreadyDisabledTable_ReturnsFalse()
    {
        // Arrange
        // Table exists but tracking is not enabled

        // Act
        // Assert
        await Assert.ThrowsAsync<SqlException>(async () =>
            await _operations.DisableTracking(_testTableName, CancellationToken.None));
    }
}
