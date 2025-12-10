using System.Reflection;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Extensions;

namespace Tracker.AspNet.Services;

public class ETagService(Assembly executionAssembly) : IETagService
{
    private readonly string _assemblyBuildTime = executionAssembly.GetAssemblyWriteTime().Ticks.ToString();

    public bool EqualsTo(string ifNoneMatch, ulong lastTimestamp, string suffix)
    {
        var ltDigitsCount = lastTimestamp.CountDigits();

        var fullLength = ComputeLength(ltDigitsCount, suffix);
        if (fullLength != ifNoneMatch.Length)
            return false;

        var etag = ifNoneMatch.AsSpan();

        var position = _assemblyBuildTime.Length;
        var eTagAssemBuildTime = etag[..position];
        if (!eTagAssemBuildTime.Equals(_assemblyBuildTime.AsSpan(), StringComparison.Ordinal))
            return false;

        var inTicks = etag.Slice(++position, ltDigitsCount);
        if (!inTicks.EqualsLong(lastTimestamp))
            return false;

        position += ltDigitsCount;
        if (position == etag.Length)
            return true;

        var inSuffix = etag[++position..];
        if (!inSuffix.Equals(suffix, StringComparison.Ordinal))
            return false;

        return true;
    }

    public string Build(ulong lastTimestamp, string suffix)
    {
        var fullLength = ComputeLength(lastTimestamp, suffix);
        return string.Create(fullLength, (_assemblyBuildTime, lastTimestamp, suffix), (chars, state) =>
        {
            var (asBuildTime, lastTimestamp, suffix) = state;

            var position = asBuildTime.Length;
            asBuildTime.AsSpan().CopyTo(chars);
            chars[position++] = '-';

            lastTimestamp.TryFormat(chars[position..], out var written);

            if (!string.IsNullOrEmpty(suffix))
            {
                position += written;
                chars[position++] = '-';
                suffix.AsSpan().CopyTo(chars[position..]);
            }
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ComputeLength(ulong lastTimestamp, string suffix) => ComputeLength(lastTimestamp.CountDigits(), suffix);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ComputeLength(int lastTimestampDigitsCount, string suffix) =>
        _assemblyBuildTime.Length + lastTimestampDigitsCount + suffix.Length + (suffix.Length > 0 ? 2 : 1);

}
