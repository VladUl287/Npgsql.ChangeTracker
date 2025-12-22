using Microsoft.EntityFrameworkCore;
using System.Buffers;
using System.IO.Hashing;
using System.Text;
using Tracker.Core.Services.Contracts;

namespace Tracker.Core.Services;

public sealed class DefaultSourceIdGenerator : IProviderIdGenerator
{
    public string GenerateId<TContext>() where TContext : DbContext
    {
        var typeName = typeof(TContext).FullName ??
            throw new NullReferenceException("Not correct type FullName for correct hash generation");

        var maximumBytes = Encoding.UTF8.GetMaxByteCount(typeName.Length);

        const int MaxBytesThreshold = 256;
        if (maximumBytes > MaxBytesThreshold)
        {
            byte[] data = ArrayPool<byte>.Shared.Rent(maximumBytes);

            var count = Encoding.UTF8.GetBytes(typeName, data);
            var bytes = data.AsSpan()[..count];

            var hash = XxHash64.HashToUInt64(bytes);

            ArrayPool<byte>.Shared.Return(data);

            return hash.ToString();
        }
        else
        {
            Span<byte> data = stackalloc byte[maximumBytes];

            var count = Encoding.UTF8.GetBytes(typeName, data);
            data = data[..count];

            return XxHash64.HashToUInt64(data).ToString();
        }
    }
}
