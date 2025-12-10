using System.Buffers;
using System.Buffers.Binary;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using Tracker.Core.Services.Contracts;

namespace Tracker.Core.Services;

public sealed class XxHash64Hasher : ITimestampsHasher
{
    public ulong Hash(ReadOnlySpan<DateTimeOffset> timestamps)
    {
        if (BitConverter.IsLittleEndian)
            return HashLittleEndian(timestamps);

        return HashBigEndian(timestamps);
    }

    private static ulong HashLittleEndian(ReadOnlySpan<DateTimeOffset> timestamps)
    {
        const int StackAllocThreshold = 128;

        var byteCount = timestamps.Length * sizeof(long);

        if (byteCount > StackAllocThreshold)
        {
            var rented = ArrayPool<long>.Shared.Rent(timestamps.Length);
            Span<long> ticksBuffer = rented.AsSpan(0, timestamps.Length);

            for (int i = 0; i < timestamps.Length; i++)
                ticksBuffer[i] = timestamps[i].Ticks;

            var hash = XxHash64.HashToUInt64(MemoryMarshal.AsBytes(ticksBuffer));
            ArrayPool<long>.Shared.Return(rented);
            return hash;
        }

        Span<long> ticks = stackalloc long[timestamps.Length];
        for (int i = 0; i < timestamps.Length; i++)
            ticks[i] = timestamps[i].Ticks;
        return XxHash64.HashToUInt64(MemoryMarshal.AsBytes(ticks));
    }

    private static ulong HashBigEndian(ReadOnlySpan<DateTimeOffset> timestamps)
    {
        const int StackAllocThreshold = 128;

        var byteCount = timestamps.Length * sizeof(long);

        if (byteCount > StackAllocThreshold)
        {
            var rented = ArrayPool<byte>.Shared.Rent(byteCount);
            Span<byte> ticks = rented.AsSpan(0, timestamps.Length);

            for (int i = 0; i < timestamps.Length; i++)
                BinaryPrimitives.WriteInt64LittleEndian(
                    ticks.Slice(i * sizeof(long), sizeof(long)),
                    timestamps[i].Ticks);

            ArrayPool<byte>.Shared.Return(rented);
            return XxHash64.HashToUInt64(ticks);
        }

        Span<byte> buffer = stackalloc byte[byteCount];
        for (int i = 0; i < timestamps.Length; i++)
            BinaryPrimitives.WriteInt64LittleEndian(
                buffer.Slice(i * sizeof(long), sizeof(long)),
                timestamps[i].Ticks);
        return XxHash64.HashToUInt64(buffer);
    }
}
