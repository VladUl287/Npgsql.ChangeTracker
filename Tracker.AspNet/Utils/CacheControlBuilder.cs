namespace Tracker.AspNet.Utils;

public sealed class CacheControlBuilder
{
    private readonly List<string> _directives = [];

    public CacheControlBuilder WithDirective(string directive)
    {
        _directives.Add(directive);
        return this;
    }

    public CacheControlBuilder WithMaxAge(TimeSpan duration) => WithDirective($"max-age={duration.TotalSeconds}");
    public CacheControlBuilder WithSMaxAge(TimeSpan duration) => WithDirective($"s-maxage={duration.TotalSeconds}");
    public CacheControlBuilder WithNoCache() => WithDirective("no-cache");
    public CacheControlBuilder WithNoStore() => WithDirective("no-store");
    public CacheControlBuilder WithNoTransform() => WithDirective("no-transform");
    public CacheControlBuilder WithMustRevalidate() => WithDirective("must-revalidate");
    public CacheControlBuilder WithProxyRevalidate() => WithDirective("proxy-revalidate");
    public CacheControlBuilder WithMustUnderstand() => WithDirective("must-understand");
    public CacheControlBuilder WithPrivate() => WithDirective("private");
    public CacheControlBuilder WithPublic() => WithDirective("public");
    public CacheControlBuilder WithImmutable() => WithDirective("immutable");
    public CacheControlBuilder WithStaleWhileRevalidate(TimeSpan duration) => WithDirective($"stale-while-revalidate={duration.TotalSeconds}");
    public CacheControlBuilder WithStaleIfError(TimeSpan duration) => WithDirective($"stale-if-error={duration.TotalSeconds}");

    public string Build() => $"Cache-Control: {string.Join(", ", _directives)}";
}
