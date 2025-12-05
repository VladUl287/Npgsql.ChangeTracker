using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tracker.AspNet.Models;
using Tracker.Core.Extensions;

namespace Tracker.AspNet.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TrackAttribute<TContext>(
    string[]? tables = null,
    string? sourceId = null,
    string? cacheControl = null) : TrackAttributeBase(tables, sourceId, cacheControl) where TContext : DbContext
{
    private ImmutableGlobalOptions? _actionOptions;
    private readonly Lock _lock = new();

    protected override ImmutableGlobalOptions GetOrSetOptions(ActionExecutingContext execContext)
    {
        if (_actionOptions is not null)
            return _actionOptions;

        lock (_lock)
        {
            if (_actionOptions is not null)
                return _actionOptions;

            var baseOptions = execContext.HttpContext.RequestServices.GetRequiredService<ImmutableGlobalOptions>();
            _actionOptions = baseOptions with
            {
                CacheControl = _cacheControl ?? baseOptions.CacheControl,
                Source = _sourceId ?? typeof(TContext).GetTypeHashId(),
                Tables = _tables
            };
            return _actionOptions;
        }
    }
}
