using BenchmarkDotNet.Attributes;
using System.Runtime.CompilerServices;

namespace Tracker.Benchmarks;

[MemoryDiagnoser]
public unsafe class ETagComparerBenchmark
{
    private static readonly string IncomingETag = "639007748579998180-631437120000000000-suffix";
    private static readonly long AssemblyBuildTime = 639007748579998180;
    private static readonly string AssemblyBuildTimeString = AssemblyBuildTime.ToString();
    private static readonly string Suffix = "suffix";
    private static readonly long LastTimestamp = 631437120000000000;

    [Benchmark]
    public string? Compare_Equal_AlwaysGenerate()
    {
        var incominigEtag = IncomingETag;

        var etag = $"{AssemblyBuildTime}-{LastTimestamp}-{Suffix}";
        if (etag == incominigEtag)
            return null;

        return etag;
    }

    [Benchmark]
    public string? Compare_Equal_PartialGenerate()
    {
        var incominigEtag = IncomingETag.AsSpan();

        var digitCount = AssemblyBuildTimeString.Length;
        var inAssemblyBuildTime = incominigEtag[..digitCount];

        var lastTimestampDigitCount = DigitCountLog(LastTimestamp);

        if (!inAssemblyBuildTime.Equals(AssemblyBuildTimeString.AsSpan(), StringComparison.Ordinal))
        {
            var fullLength = AssemblyBuildTimeString.Length + 2 + lastTimestampDigitCount + Suffix.Length;
            return string.Create(fullLength, (AssemblyBuildTimeString, LastTimestamp, Suffix), (chars, state) =>
            {
                state.AssemblyBuildTimeString.AsSpan().CopyTo(chars);
                var position = state.AssemblyBuildTimeString.Length;
                chars[position++] = '-';

                state.LastTimestamp.TryFormat(chars[position..], out var written);
                position += written;

                chars[position++] = '-';

                state.Suffix.AsSpan().CopyTo(chars[position..]);
            });
        }

        var start = digitCount + 1;
        var inTicks = incominigEtag.Slice(start, lastTimestampDigitCount);

        if (!FastCompareStringToLongSimd(inTicks, LastTimestamp))
        {
            var fullLength = AssemblyBuildTimeString.Length + 2 + lastTimestampDigitCount + Suffix.Length;
            return string.Create(fullLength, (AssemblyBuildTimeString, LastTimestamp, Suffix), (chars, state) =>
            {
                state.AssemblyBuildTimeString.AsSpan().CopyTo(chars);
                var position = state.AssemblyBuildTimeString.Length;
                chars[position++] = '-';

                state.LastTimestamp.TryFormat(chars[position..], out var written);
                position += written;

                chars[position++] = '-';

                state.Suffix.AsSpan().CopyTo(chars[position..]);
            });
        }

        start += lastTimestampDigitCount + 1;
        var inSuffix = incominigEtag[start..];

        if (!inSuffix.Equals(Suffix, StringComparison.Ordinal))
        {
            var fullLength = AssemblyBuildTimeString.Length + 2 + lastTimestampDigitCount + Suffix.Length;
            return string.Create(fullLength, (AssemblyBuildTimeString, LastTimestamp, Suffix), (chars, state) =>
            {
                state.AssemblyBuildTimeString.AsSpan().CopyTo(chars);
                var position = state.AssemblyBuildTimeString.Length;
                chars[position++] = '-';

                state.LastTimestamp.TryFormat(chars[position..], out var written);
                position += written;

                chars[position++] = '-';

                state.Suffix.AsSpan().CopyTo(chars[position..]);
            });
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool FastCompareStringToLongSimd(ReadOnlySpan<char> str, long number)
    {
        if (str.Length > 19)
            return false;

        long result = 0;
        foreach (var c in str)
        {
            //if (c < '0' || c > '9') return false;
            result = result * 10 + (c - '0');
        }

        return result == number;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DigitCountLog(long n)
    {
        if (n == 0) return 1;
        return (int)Math.Floor(Math.Log10(n)) + 1;
    }
}
