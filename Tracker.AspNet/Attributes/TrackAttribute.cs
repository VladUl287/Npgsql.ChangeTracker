using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using Tracker.AspNet.Models;

namespace Tracker.AspNet.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TrackAttribute(
    string[]? tables = null,
    string? sourceId = null,
    string? cacheControl = null) : TrackAttributeBase
{
    private ImmutableGlobalOptions? _actionOptions;
    private readonly Lock _lock = new();

    protected override ImmutableGlobalOptions GetOptions(ActionExecutingContext execCtx)
    {
        if (_actionOptions is not null)
            return _actionOptions;

        lock (_lock)
        {
            if (_actionOptions is not null)
                return _actionOptions;

            var options = execCtx.HttpContext.RequestServices.GetRequiredService<ImmutableGlobalOptions>();
            return _actionOptions = options with
            {
                CacheControl = cacheControl ?? options.CacheControl,
                Source = sourceId ?? options.Source,
                Tables = tables?.ToImmutableArray() ?? []
            };
        }
    }
}