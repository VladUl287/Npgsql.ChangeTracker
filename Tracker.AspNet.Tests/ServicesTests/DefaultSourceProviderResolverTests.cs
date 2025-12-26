using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Tests.ServicesTests;

public class DefaultProviderResolverTests
{
    private readonly Mock<ILogger<DefaultProviderResolver>> _loggerMock;
    private readonly DefaultProviderResolver _resolver;

    public DefaultProviderResolverTests()
    {
        _loggerMock = new Mock<ILogger<DefaultProviderResolver>>();
        _resolver = new DefaultProviderResolver(_loggerMock.Object);
    }

    [Fact]
    public void ResolveProvider_ShouldThrowArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var options = new ImmutableGlobalOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => _resolver.ResolveProvider(null, options, out _));
    }

    [Fact]
    public void ResolveProvider_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        // Arrange
        var httpContextMock = new Mock<HttpContext>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => _resolver.ResolveProvider(httpContextMock.Object, null, out _));
    }

    [Fact]
    public void ResolveProvider_ShouldResolveKeyedProvider_WhenProviderIdIsSpecified()
    {
        // Arrange
        var providerId = "TestProvider";
        var options = new ImmutableGlobalOptions { ProviderId = providerId };
        var expectedProvider = Mock.Of<ISourceProvider>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var httpContextMock = new Mock<HttpContext>();

        serviceProviderMock
            .As<IKeyedServiceProvider>()
            .Setup(sp => sp.GetRequiredKeyedService(typeof(ISourceProvider), providerId))
            .Returns(expectedProvider);

        httpContextMock.Setup(ctx => ctx.RequestServices)
            .Returns(serviceProviderMock.Object);

        // Act
        var result = _resolver.ResolveProvider(httpContextMock.Object, options, out var shouldDispose);

        // Assert
        Assert.Equal(expectedProvider, result);
        Assert.False(shouldDispose);
    }

    [Fact]
    public void ResolveProvider_ShouldThrowInvalidOperationException_WhenKeyedProviderResolutionFails()
    {
        // Arrange
        var providerId = "TestProvider";
        var options = new ImmutableGlobalOptions { ProviderId = providerId };
        var serviceProviderMock = new Mock<IServiceProvider>();
        var httpContextMock = new Mock<HttpContext>();
        var expectedException = new InvalidOperationException("Service not found");

        serviceProviderMock
            .As<IKeyedServiceProvider>()
            .Setup(sp => sp.GetRequiredKeyedService(typeof(ISourceProvider), providerId))
            .Throws(expectedException);

        httpContextMock.Setup(ctx => ctx.RequestServices)
            .Returns(serviceProviderMock.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _resolver.ResolveProvider(httpContextMock.Object, options, out _));

        Assert.Contains($"Failed to resolve source provider. TraceId:", exception.Message);
        Assert.Equal(expectedException, exception.InnerException);
    }

    [Fact]
    public void ResolveProvider_ShouldReturnDirectProviderInstance_WhenSourceProviderIsSpecified()
    {
        // Arrange
        var expectedProvider = Mock.Of<ISourceProvider>();
        var options = new ImmutableGlobalOptions { SourceProvider = expectedProvider };
        var httpContextMock = new Mock<HttpContext>();

        // Act
        var result = _resolver.ResolveProvider(httpContextMock.Object, options, out var shouldDispose);

        // Assert
        Assert.Equal(expectedProvider, result);
        Assert.False(shouldDispose);
    }

    [Fact]
    public void ResolveProvider_ShouldCreateProviderViaFactory_WhenSourceProviderFactoryIsSpecified()
    {
        // Arrange
        var expectedProvider = Mock.Of<ISourceProvider>();
        var options = new ImmutableGlobalOptions
        {
            SourceProviderFactory = ctx => expectedProvider
        };
        var httpContextMock = new Mock<HttpContext>();

        // Act
        var result = _resolver.ResolveProvider(httpContextMock.Object, options, out var shouldDispose);

        // Assert
        Assert.Equal(expectedProvider, result);
        Assert.True(shouldDispose);
    }

    [Fact]
    public void ResolveProvider_ShouldResolveLastRegisteredProvider_WhenNoOtherOptionsAreSpecified()
    {
        // Arrange
        var options = new ImmutableGlobalOptions();
        var expectedProvider = Mock.Of<ISourceProvider>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var httpContextMock = new Mock<HttpContext>();

        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ISourceProvider)))
            .Returns(expectedProvider);

        httpContextMock.Setup(ctx => ctx.RequestServices)
            .Returns(serviceProviderMock.Object);

        // Act
        var result = _resolver.ResolveProvider(httpContextMock.Object, options, out var shouldDispose);

        // Assert
        Assert.Equal(expectedProvider, result);
        Assert.False(shouldDispose);
    }

    [Fact]
    public void ResolveProvider_ShouldPrioritizeProviderId_WhenMultipleOptionsAreSpecified()
    {
        // Arrange
        var providerId = "TestProvider";
        var directProvider = Mock.Of<ISourceProvider>();
        var factoryProvider = Mock.Of<ISourceProvider>();

        var options = new ImmutableGlobalOptions
        {
            ProviderId = providerId,
            SourceProvider = directProvider,
            SourceProviderFactory = (ctx) => factoryProvider
        };

        var expectedProvider = Mock.Of<ISourceProvider>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var httpContextMock = new Mock<HttpContext>();

        serviceProviderMock
            .As<IKeyedServiceProvider>()
            .Setup(sp => sp.GetRequiredKeyedService(typeof(ISourceProvider), providerId))
            .Returns(expectedProvider);

        httpContextMock.Setup(ctx => ctx.RequestServices)
            .Returns(serviceProviderMock.Object);

        // Act
        var result = _resolver.ResolveProvider(httpContextMock.Object, options, out var shouldDispose);

        // Assert
        Assert.Equal(expectedProvider, result);
        Assert.False(shouldDispose);
    }

    [Fact]
    public void ResolveProvider_ShouldPrioritizeDirectProvider_WhenNoProviderIdButSourceProviderExists()
    {
        // Arrange
        var expectedProvider = Mock.Of<ISourceProvider>();
        var factoryProvider = Mock.Of<ISourceProvider>();

        var options = new ImmutableGlobalOptions
        {
            SourceProvider = expectedProvider,
            SourceProviderFactory = (ctx) => factoryProvider
        };

        var httpContextMock = new Mock<HttpContext>();

        // Act
        var result = _resolver.ResolveProvider(httpContextMock.Object, options, out var shouldDispose);

        // Assert
        Assert.Equal(expectedProvider, result);
        Assert.False(shouldDispose);
    }
}
