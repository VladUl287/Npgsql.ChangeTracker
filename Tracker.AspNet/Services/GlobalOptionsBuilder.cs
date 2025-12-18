using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Logging;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;
using Tracker.AspNet.Utils;
using Tracker.Core.Extensions;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Services;

public sealed class GlobalOptionsBuilder(IServiceScopeFactory scopeFactory) : IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>
{
    private static readonly string _defaultCacheControl = new CacheControlBuilder().WithNoCache().Combine();

    public ImmutableGlobalOptions Build(GlobalOptions options)
    {
        return new ImmutableGlobalOptions
        {
            Source = options.Source,
            Suffix = options.Suffix,
            Filter = options.Filter,
            InvalidRequestDirectives = [.. options.InvalidRequestDirectives],
            InvalidResponseDirectives = [.. options.InvalidResponseDirectives],
            Tables = [.. options.Tables],
            SourceOperations = options.SourceOperations,
            CacheControl = ResolveCacheControl(options),
            SourceOperationsFactory = options.SourceOperationsFactory,
        };
    }

    public ImmutableGlobalOptions Build<TContext>(GlobalOptions options) where TContext : DbContext
    {
        using var scope = scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        var sourceIdGenerator = scope.ServiceProvider.GetRequiredService<ISourceIdGenerator>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<GlobalOptionsBuilder>>();

        var tables = GetAndCombineTablesNames(options, dbContext, logger);

        var sourceId = options.Source;
        if (options is { Source: null, SourceOperations: null, SourceOperationsFactory: null })
            sourceId = sourceIdGenerator.GenerateId<TContext>();

        return new ImmutableGlobalOptions
        {
            Tables = tables,
            Source = sourceId,
            Suffix = options.Suffix,
            Filter = options.Filter,
            InvalidRequestDirectives = [.. options.InvalidRequestDirectives],
            InvalidResponseDirectives = [.. options.InvalidResponseDirectives],
            CacheControl = ResolveCacheControl(options),
            SourceOperations = options.SourceOperations,
            SourceOperationsFactory = options.SourceOperationsFactory,
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ResolveCacheControl(GlobalOptions options) =>
        options.CacheControl ?? options.CacheControlBuilder?.Combine() ?? _defaultCacheControl;

    private static ImmutableArray<string> GetAndCombineTablesNames<TContext>(
        GlobalOptions options, TContext dbContext, ILogger<GlobalOptionsBuilder> logger) where TContext : DbContext
    {
        var tablesNames = new HashSet<string>();
        foreach (var tableName in options.Tables ?? [])
            if (!tablesNames.Add(tableName))
                logger.LogOptionsTableNameDuplicated(tableName);

        foreach (var tableName in dbContext.GetTablesNames(options.Entities ?? []))
            if (!tablesNames.Add(tableName))
                logger.LogOptionsTableNameDuplicated(tableName);

        return [.. tablesNames];
    }
}
