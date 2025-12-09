using System.IO.Hashing;
using Tracker.Core.Services.Contracts;

namespace Tracker.Core.Services;

public sealed class XxHash32Hasher : ITimestampsHasher
{
    public ulong Hash(Span<DateTimeOffset> timestamps)
    {
        var hash = new XxHash32();

        foreach (var tmsmp in timestamps)
            hash.Append(BitConverter.GetBytes(tmsmp.Ticks));

        return hash.GetCurrentHashAsUInt32();
    }
}
