using Npgsql;
using System.Data;
using Tracker.Core.Services.Contracts;

namespace Tracker.Npgsql.Services;

public sealed class NpgsqlOperations : ISourceOperations
{
    private readonly string sourceId;
    private readonly NpgsqlDataSource dataSource;

    public NpgsqlOperations(string sourceId, NpgsqlDataSource dataSource)
    {
        this.sourceId = sourceId;
        this.dataSource = dataSource;
    }

    public NpgsqlOperations(string sourceId, string connectionString)
    {
        this.sourceId = sourceId;
        dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();
    }

    public string SourceId => sourceId;

    public async Task<DateTimeOffset?> GetLastTimestamp(string key, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        const string getTimestampQuery = "SELECT get_last_timestamp(@table_name);";
        await using var command = dataSource.CreateCommand(getTimestampQuery);

        const string tableNameParam = "table_name";
        command.Parameters.AddWithValue(tableNameParam, key);

        using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);

        if (await reader.ReadAsync(token))
            return await reader.GetFieldValueAsync<DateTimeOffset?>(0, token);

        return null;
    }

    public Task<IEnumerable<DateTimeOffset>> GetLastTimestamp(string[] keys, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<DateTimeOffset?> GetLastTimestamp(CancellationToken token)
    {
        throw new NotImplementedException();
    }
}
