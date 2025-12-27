using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Logging;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Services;

public sealed class DefaultProviderResolver(ILogger<DefaultProviderResolver> logger) : IProviderResolver
{
    private static string? _defaultProviderId;
    private static readonly Lock _lock = new();

    public ISourceProvider ResolveProvider(HttpContext ctx, ImmutableGlobalOptions options, out bool shouldDispose)
    {
        ArgumentNullException.ThrowIfNull(ctx, nameof(ctx));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        var traceId = new TraceId(ctx);
        try
        {
            shouldDispose = false;

            if (options.ProviderId is not null)
            {
                logger.LogResolvingKeyedProvider(options.ProviderId, traceId);
                return ctx.RequestServices.GetRequiredKeyedService<ISourceProvider>(options.ProviderId);
            }

            if (options.SourceProvider is not null)
            {
                logger.LogUsingDirectProviderInstance(traceId);
                return options.SourceProvider;
            }

            if (options.SourceProviderFactory is not null)
            {
                logger.LogCreatingProviderViaFactory(traceId);
                shouldDispose = true;
                return options.SourceProviderFactory(ctx);
            }

            logger.LogResolvingLastRegisteredProvider(traceId);
            return GetDefaultProvider(ctx);
        }
        catch (Exception ex)
        {
            logger.LogFailedToResolveSourceProvider(ex, traceId);
            throw new InvalidOperationException(
                $"Failed to resolve source provider. TraceId: {ctx.TraceIdentifier}", ex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ISourceProvider GetDefaultProvider(HttpContext ctx)
    {
        if (_defaultProviderId is not null)
            return ctx.RequestServices.GetRequiredKeyedService<ISourceProvider>(_defaultProviderId);

        lock (_lock)
        {
            if (_defaultProviderId is not null)
                return ctx.RequestServices.GetRequiredKeyedService<ISourceProvider>(_defaultProviderId);

            var firstProvider = ctx.RequestServices
                .GetKeyedServices<ISourceProvider>(KeyedService.AnyKey)
                .First();

            _defaultProviderId = firstProvider.Id;
            return firstProvider;
        }
    }
}
