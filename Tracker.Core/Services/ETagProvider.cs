using System.Runtime.CompilerServices;
using Tracker.Core.Extensions;
using Tracker.Core.Services.Contracts;

[assembly: InternalsVisibleTo("Tracker.Core.Tests")]

namespace Tracker.Core.Services;

public class ETagProvider(IAssemblyTimestampProvider assemblyTimestampProvider) : IETagProvider
{
    private readonly string _assemblyTimestamp = assemblyTimestampProvider.GetWriteTime().Ticks.ToString();

    public bool Compare(string etag, ulong lastTimestamp, string suffix)
    {
        var timestampDigitCount = lastTimestamp.CountDigits();
        var expectedLength = CalculateEtagLength(timestampDigitCount, suffix.Length);

        if (expectedLength != etag.Length)
            return false;

        var etagSpan = etag.AsSpan();
        var position = _assemblyTimestamp.Length;

        var assemblyTimestampSegment = etagSpan[..position];
        if (!assemblyTimestampSegment.Equals(_assemblyTimestamp.AsSpan(), StringComparison.Ordinal))
            return false;

        if (etagSpan[position] != '-')
            return false;

        var timestampSegment = etagSpan.Slice(++position, timestampDigitCount);
        if (!timestampSegment.EqualsULong(lastTimestamp))
            return false;

        position += timestampDigitCount;

        if (position == etagSpan.Length)
            return suffix.Length == 0;

        if (etagSpan[position] != '-')
            return false;

        var suffixSegment = etagSpan[++position..];
        return suffixSegment.Equals(suffix, StringComparison.Ordinal);
    }

    public string Generate(ulong lastTimestamp, string suffix)
    {
        var timestampDigitCount = lastTimestamp.CountDigits();
        var totalLength = CalculateEtagLength(timestampDigitCount, suffix.Length);

        return string.Create(totalLength, (_assemblyTimestamp, lastTimestamp, suffix), (chars, state) =>
        {
            var (assemblyTimestamp, timestamp, suffix) = state;

            var position = assemblyTimestamp.Length;
            assemblyTimestamp.AsSpan().CopyTo(chars);

            chars[position++] = '-';

            timestamp.TryFormat(chars[position..], out var digitsWritten);
            position += digitsWritten;

            if (suffix.Length > 0)
            {
                chars[position++] = '-';
                suffix.AsSpan().CopyTo(chars[position..]);
            }
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int CalculateEtagLength(int timestampDigitCount, int suffixLength)
    {
        var separatorCount = suffixLength > 0 ? 2 : 1;
        return _assemblyTimestamp.Length + timestampDigitCount + suffixLength + separatorCount;
    }
}
