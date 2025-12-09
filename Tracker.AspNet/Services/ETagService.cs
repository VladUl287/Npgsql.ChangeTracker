using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Logging;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Services;

public class ETagService(
    IETagGenerator etagGenerator, ISourceOperationsResolver operationsResolver, ILogger<ETagService> logger) : IETagService
{
    public async Task<bool> TrySetETagAsync(HttpContext ctx, ImmutableGlobalOptions options, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(ctx, nameof(ctx));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        var sourceOperations = GetOperationsProvider(ctx, options, operationsResolver);

        var ltValue = 0L;
        if (options is { Tables.Length: 0 })
        {
            var tm = await sourceOperations.GetLastTimestamp(token);
            ltValue = tm.Ticks;
        }
        else
        {
            var timestamps = ArrayPool<DateTimeOffset>.Shared.Rent(options.Tables.Length);
            await sourceOperations.GetLastTimestamps(options.Tables, timestamps, token);

            foreach (var tmsmp in timestamps)
                ltValue ^= tmsmp.Ticks;

            ArrayPool<DateTimeOffset>.Shared.Return(timestamps);
        }

        var incomingETag = ctx.Request.Headers.IfNoneMatch.Count > 0 ? ctx.Request.Headers.IfNoneMatch[0] : null;

        var asBuildTime = etagGenerator.AssemblyBuildTimeTicks;
        var ltDigitCount = DigitCountLog(ltValue);
        var suffix = options.Suffix(ctx);
        var fullLength = asBuildTime.Length + 2 + ltDigitCount + suffix.Length;

        if (incomingETag is not null && ETagEqual(incomingETag, ltValue, asBuildTime, suffix))
        {
            ctx.Response.StatusCode = StatusCodes.Status304NotModified;
            logger.LogNotModified(incomingETag);
            return true;
        }

        ctx.Response.Headers.CacheControl = options.CacheControl;
        var etag = BuildETag(fullLength, (asBuildTime, ltValue, suffix));
        ctx.Response.Headers.ETag = etag;
        logger.LogETagAdded(etag);
        return false;
    }

    private static bool ETagEqual(string inETag, long lTimestamp, string asBuildTime, string suffix)
    {
        var ltDigitCount = DigitCountLog(lTimestamp);

        var fullLength = asBuildTime.Length + 2 + ltDigitCount + suffix.Length;
        if (fullLength != inETag.Length)
            return false;

        var incomingETag = inETag.AsSpan();
        var rightEdge = asBuildTime.Length;
        var inAsBuildTime = incomingETag[..rightEdge];
        if (!inAsBuildTime.Equals(asBuildTime.AsSpan(), StringComparison.Ordinal))
            return false;

        var inTicks = incomingETag.Slice(++rightEdge, ltDigitCount);
        if (!CompareStringWithLong(inTicks, lTimestamp))
            return false;

        var inSuffix = incomingETag[rightEdge..];
        if (!inSuffix.Equals(suffix, StringComparison.Ordinal))
            return false;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CompareStringWithLong(ReadOnlySpan<char> str, long number)
    {
        if (str.Length > 19)
            return false;

        long result = 0;
        foreach (var c in str)
        {
            if (c < '0' || c > '9') return false;
            result = result * 10 + (c - '0');
        }

        return result == number;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string BuildETag(int fullLength, (string AsBuldTime, long LastTimestamp, string Suffix) state) =>
        string.Create(fullLength, state, (chars, state) =>
        {
            var position = state.AsBuldTime.Length;
            state.AsBuldTime.AsSpan().CopyTo(chars);
            chars[position++] = '-';

            state.LastTimestamp.TryFormat(chars[position..], out var written);
            position += written;
            chars[position++] = '-';

            state.Suffix.AsSpan().CopyTo(chars[position..]);
        });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int DigitCountLog(long n)
    {
        if (n == 0) return 1;
        return (int)Math.Floor(Math.Log10(n)) + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ISourceOperations GetOperationsProvider(
        HttpContext ctx, ImmutableGlobalOptions opt, ISourceOperationsResolver resolver) =>
        opt.SourceOperations ?? opt.SourceOperationsFactory?.Invoke(ctx) ?? resolver.Resolve(opt.Source);
}
