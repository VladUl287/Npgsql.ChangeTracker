using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Npgsql;
using System.Collections.Immutable;
using System.Data;
using Tracker.Core.Services.Contracts;

namespace Tracker.Npgsql.Services;

public sealed class NpgsqlOperations : ISourceOperations, IDisposable
{
    private readonly string _sourceId;
    private readonly NpgsqlDataSource _dataSource;
    private bool _disposed;

    private const string TABLE_NAME_PARAM = "table_name";
    private const string TIMESTAMP_PARAM = "timestamp";

    public NpgsqlOperations(string sourceId, NpgsqlDataSource dataSource)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId, nameof(sourceId));
        ArgumentNullException.ThrowIfNull(dataSource, nameof(dataSource));

        _sourceId = sourceId;
        _dataSource = dataSource;
    }

    public NpgsqlOperations(string sourceId, string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId, nameof(sourceId));
        ArgumentException.ThrowIfNullOrEmpty(connectionString, nameof(connectionString));

        _sourceId = sourceId;
        _dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();
    }

    public string SourceId => _sourceId;

    public ValueTask<bool> EnableTracking(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string EnableTableTracking = "SELECT enable_table_tracking(@table_name);";
        using var command = _dataSource.CreateCommand(EnableTableTracking);
        command.Parameters.AddWithValue(TABLE_NAME_PARAM, key);

        using var reader = command.ExecuteReader(CommandBehavior.SingleRow);
        var enabled = reader.Read() && reader.GetFieldValue<bool>(0);
        return new ValueTask<bool>(enabled);
    }
    public ValueTask<bool> DisableTracking(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string DisableTableQuery = "SELECT disable_table_tracking(@table_name);";
        using var command = _dataSource.CreateCommand(DisableTableQuery);
        command.Parameters.AddWithValue(TABLE_NAME_PARAM, key);

        using var reader = command.ExecuteReader(CommandBehavior.SingleRow);
        var disabled = reader.Read() && reader.GetFieldValue<bool>(0);
        return new ValueTask<bool>(disabled);
    }

    public ValueTask<bool> IsTracking(string key, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string IsTrackingQuery = "SELECT is_table_tracked(@table_name);";
        using var command = _dataSource.CreateCommand(IsTrackingQuery);
        command.Parameters.AddWithValue(TABLE_NAME_PARAM, key);

        using var reader = command.ExecuteReader(CommandBehavior.SingleRow);
        var tracking = reader.Read() && reader.GetFieldValue<bool>(0);
        return new ValueTask<bool>(tracking);
    }

    public ValueTask<DateTimeOffset> GetLastTimestamp(string key, CancellationToken token = default)
    {
        const string GetTimestampQuery = "SELECT get_last_timestamp(@table_name);";
        using var command = _dataSource.CreateCommand(GetTimestampQuery);
        command.Parameters.AddWithValue(TABLE_NAME_PARAM, key);

        using var reader = command.ExecuteReader(CommandBehavior.SingleRow);
        if (reader.Read())
        {
            var timestamp = reader.GetFieldValue<DateTimeOffset?>(0)
               ?? throw new NullReferenceException($"Not able to resolve timestamp for table '{key}'");

            return new ValueTask<DateTimeOffset>(timestamp);
        }
        throw new InvalidOperationException($"Not able to resolve timestamp for table '{key}'");
    }

    public ValueTask GetLastTimestamps(ImmutableArray<string> keys, DateTimeOffset[] timestamps, CancellationToken token = default)
    {
        const string GetTimestampQuery = "SELECT get_last_timestamps(@table_name);";
        using var command = _dataSource.CreateCommand(GetTimestampQuery);
        command.Parameters.AddWithValue(TABLE_NAME_PARAM, NpgsqlTypes.NpgsqlDbType.Array, keys);

        using var reader = command.ExecuteReader(CommandBehavior.SingleRow);
        if (reader.Read())
        {
            var timestampsResult = reader.GetFieldValue<DateTimeOffset[]>(0);
            timestampsResult.CopyTo(timestamps, 0);
            return ValueTask.CompletedTask;
        }

        throw new InvalidOperationException($"Not able to resolve timestamp for tables");
    }

    public ValueTask<DateTimeOffset> GetLastTimestamp(CancellationToken token = default)
    {
        const string GetTimestampQuery = "SELECT pg_last_committed_xact();";
        using var command = _dataSource.CreateCommand(GetTimestampQuery);

        using var reader = command.ExecuteReader(CommandBehavior.SingleRow);
        if (reader.Read())
        {
            var result = reader.GetFieldValue<object[]?>(0);
            if (result is { Length: > 0 })
                return new ValueTask<DateTimeOffset>((DateTime)result[1]);
        }
        throw new InvalidOperationException("Not able to resolve pg_last_committed_xact");
    }

    public ValueTask<bool> SetLastTimestamp(string key, DateTimeOffset value, CancellationToken token = default)
    {
        const string SetTimestampQuery = $"SELECT set_last_timestamp(@table_name, @timestamp);";
        using var command = _dataSource.CreateCommand(SetTimestampQuery);
        command.Parameters.AddWithValue(TABLE_NAME_PARAM, key);
        command.Parameters.AddWithValue(TIMESTAMP_PARAM, value);

        using var reader = command.ExecuteReader(CommandBehavior.SingleRow);
        var setted = reader.Read() && reader.GetFieldValue<bool>(0);
        return new ValueTask<bool>(setted);
    }

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

    ~NpgsqlOperations()
    {
        Dispose(disposing: false);
    }
}
