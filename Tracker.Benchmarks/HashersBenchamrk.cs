using BenchmarkDotNet.Attributes;
using Tracker.Core.Services;

namespace Tracker.Benchmarks;

[MemoryDiagnoser]
public class HashersBenchamrk
{
    private static readonly DateTimeOffset[] _dateTimestamps = [.. Enumerable.Range(0, 5).Select(i => DateTimeOffset.UtcNow.AddDays(i))];

    public static XxHash64Hasher XxHash64Hasher = new();

    [Benchmark]
    public ulong XxHash64_Hasher()
    {
        return XxHash64Hasher.Hash(_dateTimestamps);
    }
}
