using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Extensions;

namespace Tracker.AspNet.Services;

public sealed class GlobalOptionsBuilder(IServiceScopeFactory scopeFactory) : IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>
{
    public ImmutableGlobalOptions Build(GlobalOptions options)
    {
        return new ImmutableGlobalOptions
        {
            Source = options.Source,
            SourceOperations = options.SourceOperations,
            Filter = options.Filter,
            Suffix = options.Suffix,
            Tables = [.. options.Tables],
            TablesCacheLifeTime = options.TablesCacheLifeTime,
            XactCacheLifeTime = options.XactCacheLifeTime,
        };
    }

    public ImmutableGlobalOptions Build<TContext>(GlobalOptions options) where TContext : DbContext
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

        var tablesNames = dbContext.GetTablesNames(options.Entities);
        var tables = new HashSet<string>([.. options.Tables, .. tablesNames]).ToImmutableArray();

        var source = options.Source;
        if (string.IsNullOrEmpty(source))
            source = typeof(TContext).GetTypeHashId();

        return new ImmutableGlobalOptions
        {
            Source = source,
            SourceOperations = options.SourceOperations,
            Filter = options.Filter,
            Suffix = options.Suffix,
            Tables = tables,
            TablesCacheLifeTime = options.TablesCacheLifeTime,
            XactCacheLifeTime = options.XactCacheLifeTime,
        };
    }
}
