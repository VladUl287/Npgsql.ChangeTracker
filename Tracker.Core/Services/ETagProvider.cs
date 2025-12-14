using System.Runtime.CompilerServices;
using Tracker.Core.Extensions;
using Tracker.Core.Services.Contracts;

[assembly: InternalsVisibleTo("Tracker.Core.Tests")]

namespace Tracker.Core.Services;

/// <summary>
/// Implementation of <see cref="IETagProvider"/> that uses assembly timestamp
/// for versioning and supports optional suffix for content-based ETags.
/// </summary>
/// <remarks>
/// This implementation uses <see cref="string.Create{TState}(int, TState, System.Buffers.SpanAction{char, TState})"/> for efficient string
/// construction and span-based operations for comparison to minimize allocations.
/// </remarks>
public sealed class ETagProvider(IAssemblyTimestampProvider assemblyTimestampProvider) : IETagProvider
{
    private readonly string _assemblyTimestamp = assemblyTimestampProvider.GetWriteTime().Ticks.ToString();

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <summary>
    /// Calculates the total length of an ETag based on timestamp digit count and suffix length.
    /// </summary>
    /// <param name="timestampDigitCount">The number of digits in the timestamp.</param>
    /// <param name="suffixLength">The length of the suffix string.</param>
    /// <returns>
    /// The total character length of the ETag including separators.
    /// </returns>
    /// <remarks>
    /// Calculation formula: assemblyTimestamp.Length + timestampDigitCount + suffixLength + separatorCount
    /// where separatorCount is 2 if suffixLength > 0 (two hyphens), otherwise 1 (one hyphen).
    /// 
    /// Marked with <see cref="MethodImplOptions.AggressiveInlining"/> for performance optimization
    /// as this is called in hot paths.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int CalculateEtagLength(int timestampDigitCount, int suffixLength)
    {
        var separatorCount = suffixLength > 0 ? 2 : 1;
        return _assemblyTimestamp.Length + timestampDigitCount + suffixLength + separatorCount;
    }
}
