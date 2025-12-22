using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Frozen;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Services;

public sealed class DefaultProviderResolver(
    IEnumerable<ISourceProvider> providers, IProviderIdGenerator idGenerator, ILogger<DefaultProviderResolver> logger) : IProviderResolver
{
    private readonly FrozenDictionary<string, ISourceProvider> _store = providers.ToFrozenDictionary(c => c.Id);
    private readonly ISourceProvider _default = providers.First();

    public ISourceProvider? SelectProvider(string? sourceId, ImmutableGlobalOptions options)
    {
        if (sourceId is not null)
        {
            if (_store.TryGetValue(sourceId, out var provider))
            {
                logger.LogInformation("");
                return provider;
            }

            throw new InvalidOperationException($"Fail to resolve operation with id - '{sourceId}'");
        }

        if (options is { SourceOperations: null, SourceOperationsFactory: null })
        {
            logger.LogInformation("");
            return _default;
        }

        logger.LogInformation("");
        return options.SourceOperations;
    }

    public ISourceProvider? SelectProvider(GlobalOptions options)
    {
        var sourceId = options.Source;

        if (sourceId is not null)
        {
            if (_store.TryGetValue(sourceId, out var provider))
            {
                logger.LogInformation("");
                return provider;
            }

            throw new InvalidOperationException($"Fail to resolve operation with id - '{sourceId}'");
        }

        if (options is { SourceOperations: null, SourceOperationsFactory: null })
        {
            logger.LogInformation("");
            return _default;
        }

        logger.LogInformation("");
        return options.SourceOperations;
    }

    public ISourceProvider? SelectProvider<TContext>(string? sourceId, ImmutableGlobalOptions options) where TContext : DbContext
    {
        if (sourceId is not null)
        {
            if (_store.TryGetValue(sourceId, out var provider))
            {
                logger.LogInformation("");
                return provider;
            }

            throw new InvalidOperationException($"Fail to resolve operation with id - '{sourceId}'");
        }

        logger.LogInformation("");

        sourceId = idGenerator.GenerateId<TContext>();

        if (_store.TryGetValue(sourceId, out var contextProvider))
        {
            logger.LogInformation("");
            return contextProvider;
        }

        if (options is { SourceOperations: null, SourceOperationsFactory: null })
        {
            logger.LogInformation("");
            return _default;
        }

        logger.LogInformation("");
        return options.SourceOperations;
    }

    public ISourceProvider? SelectProvider<TContext>(GlobalOptions options) where TContext : DbContext
    {
        var sourceId = options.Source;

        if (sourceId is not null)
        {
            if (_store.TryGetValue(sourceId, out var provider))
            {
                logger.LogInformation("");
                return provider;
            }

            throw new InvalidOperationException($"Fail to resolve operation with id - '{sourceId}'");
        }

        logger.LogInformation("");

        sourceId = idGenerator.GenerateId<TContext>();

        logger.LogInformation("");

        if (_store.TryGetValue(sourceId, out var contextProvider))
        {
            logger.LogInformation("");
            return contextProvider;
        }

        if (options is { SourceOperations: null, SourceOperationsFactory: null })
        {
            logger.LogInformation("");
            return _default;
        }

        logger.LogInformation("");
        return options.SourceOperations;
    }
}
