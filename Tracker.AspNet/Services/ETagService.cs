using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
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
    public async Task<bool> TrySetETagAsync(HttpContext context, ImmutableGlobalOptions options, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        var etag = await GenerateETag(context, options, token);
        if (etag is null)
        {
            logger.LogLastTimestampNotFound();
            return false;
        }

        if (context.Request.Headers.IfNoneMatch == etag)
        {
            context.Response.StatusCode = StatusCodes.Status304NotModified;

            logger.LogNotModified(etag);
            return true;
        }

        context.Response.Headers.CacheControl = options.CacheControl;
        context.Response.Headers.ETag = etag;

        logger.LogETagAdded(etag);
        return false;
    }

    private async Task<bool> GenerateETagAndCompare(HttpContext ctx, ImmutableGlobalOptions options, CancellationToken token)
    {
        var sourceOperations = ResolveOperationsProvider(ctx, options, operationsResolver);

        var lastTimestamp = await sourceOperations.GetLastTimestamp(options.Tables[0], token);
        if (lastTimestamp is null)
            return false;

        var ltValue = lastTimestamp.Value.Ticks;
        var ltDigitCount = DigitCountLog(ltValue);

        var asBuildTime = etagGenerator.AssemblyBuildTimeTicks;
        var suffix = options.Suffix(ctx);

        var incomingETag = ctx.Request.Headers.IfNoneMatch[0].AsSpan();

        var fullLength = asBuildTime.Length + 2 + ltDigitCount + suffix.Length;

        var rightEdge = asBuildTime.Length;
        var inAsBuildTime = incomingETag[..rightEdge];
        if (!inAsBuildTime.Equals(asBuildTime.AsSpan(), StringComparison.Ordinal))
        {
            ctx.Response.Headers.CacheControl = options.CacheControl;
            ctx.Response.Headers.ETag = BuildETag(fullLength, (asBuildTime, ltValue, suffix));
            return false;
        }

        var inTicks = incomingETag.Slice(++rightEdge, ltDigitCount);
        if (!CompareStringWithLong(inTicks, ltValue))
        {
            ctx.Response.Headers.CacheControl = options.CacheControl;
            ctx.Response.Headers.ETag = BuildETag(fullLength, (asBuildTime, ltValue, suffix));
            return false;
        }

        var inSuffix = incomingETag[rightEdge..];
        if (!inSuffix.Equals(suffix, StringComparison.Ordinal))
        {
            ctx.Response.Headers.CacheControl = options.CacheControl;
            ctx.Response.Headers.ETag = BuildETag(fullLength, (asBuildTime, ltValue, suffix));
            return false;
        }

        ctx.Response.StatusCode = StatusCodes.Status304NotModified;
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

    private async Task<string?> GenerateETag(HttpContext ctx, ImmutableGlobalOptions options, CancellationToken token)
    {
        var sourceOperations = ResolveOperationsProvider(ctx, options, operationsResolver);

        var suffix = options.Suffix(ctx);
        if (options is { Tables.Length: 0 })
        {
            var timestamp = await sourceOperations.GetLastTimestamp(token);
            return etagGenerator.GenerateETag(timestamp.Value, suffix);
        }

        var timestamps = ArrayPool<DateTimeOffset>.Shared.Rent(options.Tables.Length);
        await sourceOperations.GetLastTimestamps(options.Tables, timestamps, token);
        var etag = etagGenerator.GenerateETag(timestamps, suffix);
        ArrayPool<DateTimeOffset>.Shared.Return(timestamps);
        return etag;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ISourceOperations ResolveOperationsProvider(
        HttpContext ctx, ImmutableGlobalOptions opt, ISourceOperationsResolver srcResolver) =>
        opt.SourceOperations ?? opt.SourceOperationsFactory?.Invoke(ctx) ?? srcResolver.Resolve(opt.Source);
}
