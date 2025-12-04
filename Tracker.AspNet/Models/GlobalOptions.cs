using Microsoft.AspNetCore.Http;
using System.Collections.Immutable;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Models;

public sealed class GlobalOptions
{
    public string? Source { get; set; }
    public ISourceOperations? SourceOperations { get; set; }

    public Func<HttpContext, bool> Filter { get; set; } = (_) => true;

    public string[] Tables { get; set; } = [];
    public Type[] Entities { get; set; } = [];

    public TimeSpan XactCacheLifeTime { get; set; }
    public TimeSpan TablesCacheLifeTime { get; set; }

    public Func<HttpContext, string> Suffix { get; set; } = (_) => string.Empty;
}

public sealed record ImmutableGlobalOptions
{
    public string? Source { get; init; }
    public ISourceOperations? SourceOperations { get; init; }

    public Func<HttpContext, bool> Filter { get; init; } = (_) => true;

    public ImmutableArray<string> Tables { get; init; } = [];

    public TimeSpan XactCacheLifeTime { get; init; }
    public TimeSpan TablesCacheLifeTime { get; init; }

    public Func<HttpContext, string> Suffix { get; init; } = (_) => string.Empty;
}