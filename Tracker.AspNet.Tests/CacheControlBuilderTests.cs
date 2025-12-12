using Tracker.AspNet.Utils;

namespace Tracker.AspNet.Tests;

public class CacheControlBuilderTests
{
    [Fact]
    public void Combine_Empty_Builder()
    {
        // Arrange
        var builder = new CacheControlBuilder();

        // Act
        var cacheControl = builder.Combine();

        // Assert
        Assert.Empty(cacheControl);
    }

    [Fact]
    public void Use_All_Helpers_Builder()
    {
        // Arrange
        var builder = new CacheControlBuilder();

        // Act
        var cacheControl = builder
            .WithImmutable()
            .WithMaxAge(TimeSpan.FromHours(1))
            .WithMustRevalidate()
            .WithMustUnderstand()
            .WithNoCache()
            .WithNoStore()
            .WithNoTransform()
            .WithPrivate()
            .WithProxyRevalidate()
            .WithPublic()
            .WithSMaxAge(TimeSpan.FromHours(1))
            .WithStaleIfError(TimeSpan.FromHours(1))
            .WithStaleWhileRevalidate(TimeSpan.FromHours(1))
            .Combine();

        const string exptected = "immutable, max-age=3600, must-revalidate, must-understand, " +
            "no-cache, no-store, no-transform, private, proxy-revalidate, " +
            "public, s-maxage=3600, stale-if-error=3600, stale-while-revalidate=3600";

        // Assert
        Assert.Equal(exptected, cacheControl);
    }

    [Fact]
    public void Custom_Directive_No_Transformation()
    {
        // Arrange
        var builder = new CacheControlBuilder();

        // Act
        var cacheControl = builder
            .WithDirective("mAx-agE=3600")
            .Combine();

        const string exptected = "mAx-agE=3600";

        // Assert
        Assert.Equal(exptected, cacheControl);
    }

    [Fact]
    public void Null_Directive()
    {
        // Arrange
        var builder = new CacheControlBuilder();

        // Act
        var getCacheControl = () =>
        {
            builder
                .WithDirective(null)
                .Combine();
        };

        // Assert
        Assert.Throws<ArgumentNullException>(getCacheControl);
    }

    [Fact]
    public void Empty_Directive()
    {
        // Arrange
        var builder = new CacheControlBuilder();

        // Act
        var getCacheControl = () =>
        {
            builder
                .WithDirective(string.Empty)
                .Combine();
        };

        // Assert
        Assert.Throws<ArgumentException>(getCacheControl);
    }
}
