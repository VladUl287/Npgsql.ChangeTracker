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
