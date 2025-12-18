using BenchmarkDotNet.Attributes;
using Tracker.Npgsql.Services;

namespace Tracker.Benchmarks;

[MemoryDiagnoser]
public class NpgsqlOperationBenchmark
{
    private readonly NpgsqlOperations _npgsqlOperations = new("1", "Host=localhost;Port=5432;Database=main;Username=postgres;Password=postgres");
    private const string _tableName = "roles";

    [Benchmark]
    public ValueTask<long> GetRolesTimestamp()
    {
        return _npgsqlOperations.GetLastVersion(_tableName, default);
    }
}
