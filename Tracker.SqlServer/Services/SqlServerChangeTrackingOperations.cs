using Microsoft.Data.SqlClient;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using Tracker.Core.Services.Contracts;

[assembly: InternalsVisibleTo("Tracker.SqlServer.Tests")]

namespace Tracker.SqlServer.Services;

public sealed class SqlServerChangeTrackingOperations : ISourceOperations, IDisposable
{
    private readonly string _sourceId;
    private readonly DbDataSource _dataSource;
    private bool _disposed;

    public SqlServerChangeTrackingOperations(string sourceId, DbDataSource dataSource)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId, nameof(sourceId));
        ArgumentNullException.ThrowIfNull(dataSource, nameof(dataSource));

        _sourceId = sourceId;
        _dataSource = dataSource;
    }

    public SqlServerChangeTrackingOperations(string sourceId, string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId, nameof(sourceId));
        ArgumentException.ThrowIfNullOrEmpty(connectionString, nameof(connectionString));

        _sourceId = sourceId;
        _dataSource = SqlClientFactory.Instance.CreateDataSource(connectionString);
    }

    public string SourceId => _sourceId;

    public async ValueTask<long> GetLastVersion(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string GetLastVersionQuery = """
            DECLARE @current_version BIGINT = CHANGE_TRACKING_CURRENT_VERSION();
            DECLARE @min_valid_version BIGINT = CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID(@table_name));
            DECLARE @last_change_version BIGINT = NULL;
            
            DECLARE @sql NVARCHAR(MAX) = N'
                SELECT TOP 1 @last_change = SYS_CHANGE_VERSION 
                FROM CHANGETABLE(CHANGES ' + QUOTENAME(@table_name) + ', 0) as c 
                ORDER BY SYS_CHANGE_VERSION DESC';
                
            DECLARE @params NVARCHAR(MAX) = N'@last_change BIGINT OUTPUT';
            EXEC sp_executesql @sql, @params, @last_change = @last_change_version OUTPUT;
            
            SELECT 
                @current_version as current_version,
                @min_valid_version as min_valid_version,
                @last_change_version as last_change_version;
            """;

        await using var command = _dataSource.CreateCommand(GetLastVersionQuery);
        var parameter = command.CreateParameter();
        parameter.ParameterName = "table_name";
        parameter.Value = key;
        parameter.DbType = DbType.String;
        command.Parameters.Add(parameter);

        try
        {
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);

            if (await reader.ReadAsync(token))
            {
                var currentVersion = reader.GetInt64(0);
                return reader.IsDBNull(2) ? currentVersion : reader.GetInt64(2);
            }

            throw new InvalidOperationException($"Table '{key}' not found or change tracking is not enabled.");
        }
        catch (SqlException sqlException)
        {
            throw new InvalidOperationException(sqlException.Message);
        }
    }

    public async ValueTask GetLastVersions(ImmutableArray<string> keys, long[] versions, CancellationToken token = default)
    {
        if (keys.Length > versions.Length)
            throw new ArgumentException($"Timestamps array length ({versions.Length}) must be at least as large as keys count ({keys.Length}).", nameof(versions));

        for (int i = 0; i < keys.Length; i++)
            versions[i] = await GetLastVersion(keys[i], token);
    }

    public async ValueTask<long> GetLastVersion(CancellationToken token = default)
    {
        const string GetCurrentVersionQuery = "SELECT CHANGE_TRACKING_CURRENT_VERSION()";

        await using var command = _dataSource.CreateCommand(GetCurrentVersionQuery);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);

        if (await reader.ReadAsync(token))
            return reader.GetInt64(0);

        throw new InvalidOperationException("Unable to retrieve change tracking version for database.");
    }

    public async ValueTask<bool> EnableTracking(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string EnableTrackingQuery = """
            IF NOT EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID(@table_name))
            BEGIN
                DECLARE @sql NVARCHAR(MAX) = N'ALTER TABLE ' + QUOTENAME(@table_name) + ' ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON)';
                EXEC sp_executesql @sql;
                SELECT 1;
            END
            ELSE
                SELECT 1;
            """;

        await using var command = _dataSource.CreateCommand(EnableTrackingQuery);
        var parameter = command.CreateParameter();
        parameter.ParameterName = "table_name";
        parameter.Value = key;
        parameter.DbType = DbType.String;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(token);
        return result is not null;
    }

    public async ValueTask<bool> IsTracking(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string checkTrackingQuery = """
            SELECT COUNT(1) 
            FROM sys.change_tracking_tables 
            WHERE object_id = OBJECT_ID(@table_name)
            """;

        await using var command = _dataSource.CreateCommand(checkTrackingQuery);
        var parameter = command.CreateParameter();
        parameter.ParameterName = "table_name";
        parameter.Value = key;
        parameter.DbType = DbType.String;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(token);
        return result != null && Convert.ToInt32(result) > 0;
    }

    public async ValueTask<bool> DisableTracking(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        try
        {
            string disableTrackingQuery = $"""
                IF EXISTS (SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID(@table_name))
                BEGIN
                    ALTER TABLE [{key}] DISABLE CHANGE_TRACKING;
                    SELECT 1;
                END
                ELSE
                    SELECT 0;
                """;

            await using var command = _dataSource.CreateCommand(disableTrackingQuery);
            var parameter = command.CreateParameter();
            parameter.ParameterName = "table_name";
            parameter.Value = key;
            parameter.DbType = DbType.String;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(token);
            return result != null && Convert.ToInt32(result) > 0;
        }
        catch
        {
            return false;
        }
    }

    public ValueTask<bool> SetLastVersion(string key, long version, CancellationToken token = default) =>
        throw new InvalidOperationException("Cannot set version. SQL Server change tracking versions are managed by the database engine.");

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
            _dataSource?.Dispose();

        _disposed = true;
    }

    ~SqlServerChangeTrackingOperations() => Dispose(disposing: false);
}
