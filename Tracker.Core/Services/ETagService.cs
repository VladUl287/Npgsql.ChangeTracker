using System.Runtime.CompilerServices;
using Tracker.Core.Extensions;
using Tracker.Core.Services.Contracts;

namespace Tracker.Core.Services;

public class ETagService(IAssemblyTimestampProvider assemblyTimestampProvider) : IETagService
{
    private readonly string _assembWriteTime = assemblyTimestampProvider.GetWriteTime().Ticks.ToString();

    public bool EqualsTo(string etag, ulong lastTimestamp, string suffix)
    {
        var lastTimestampDigitsCount = lastTimestamp.CountDigits();

        var fullLength = ComputeLength(lastTimestampDigitsCount, suffix.Length);
        if (fullLength != etag.Length)
            return false;

        var position = _assembWriteTime.Length;

        var etagSpan = etag.AsSpan();
        var assemblyWriteTimeSlice = etagSpan[..position];
        if (!assemblyWriteTimeSlice.Equals(_assembWriteTime.AsSpan(), StringComparison.Ordinal))
            return false;

        var lastTimestampSlice = etagSpan.Slice(++position, lastTimestampDigitsCount);
        if (!lastTimestampSlice.EqualsULong(lastTimestamp))
            return false;

        position += lastTimestampDigitsCount;
        if (position == etagSpan.Length)
            return true;

        var suffixSlice = etagSpan[++position..];
        if (!suffixSlice.Equals(suffix, StringComparison.Ordinal))
            return false;

        return true;
    }

    public string Build(ulong lastTimestamp, string suffix)
    {
        var fullLength = ComputeLength(lastTimestamp.CountDigits(), suffix.Length);
        return string.Create(fullLength, (_assembWriteTime, lastTimestamp, suffix), (chars, state) =>
        {
            var (asBuildTime, lastTimestamp, suffix) = state;

            var position = asBuildTime.Length;
            asBuildTime.AsSpan().CopyTo(chars);
            chars[position++] = '-';

            lastTimestamp.TryFormat(chars[position..], out var written);
            position += written;

            if (position < chars.Length)
            {
                chars[position++] = '-';
                suffix.AsSpan().CopyTo(chars[position..]);
            }
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ComputeLength(int ltDigitsCount, int suffixLength) =>
        _assembWriteTime.Length + ltDigitsCount + suffixLength + (suffixLength > 0 ? 2 : 1);
}
