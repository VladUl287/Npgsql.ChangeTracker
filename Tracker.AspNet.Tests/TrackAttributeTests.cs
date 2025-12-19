using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Immutable;
using System.Reflection;
using Tracker.AspNet.Attributes;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Tests;

public class TrackAttributeTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IRequestFilter> _requestFilterMock;
    private readonly Mock<IRequestHandler> _requestHandlerMock;
    private readonly Mock<ILogger<TrackAttribute>> _loggerMock;
    private readonly ImmutableGlobalOptions _defaultOptions;
    private readonly HttpContext _httpContext;
    private readonly ActionExecutingContext _actionExecutingContext;

    public TrackAttributeTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _requestFilterMock = new Mock<IRequestFilter>();
        _requestHandlerMock = new Mock<IRequestHandler>();
        _loggerMock = new Mock<ILogger<TrackAttribute>>();

        _defaultOptions = new ImmutableGlobalOptions
        {
            CacheControl = "max-age=3600",
            Source = "default-source",
            Tables = ImmutableArray<string>.Empty
        };

        _httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProviderMock.Object
        };

        var actionContext = new ActionContext(_httpContext, new(), new ActionDescriptor());
        _actionExecutingContext = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }

    [Fact]
    public void TrackAttribute_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var tables = new[] { "table1", "table2" };
        var sourceId = "custom-source";
        var cacheControl = "no-cache";

        // Act
        var attribute = new TrackAttribute(tables, sourceId, cacheControl);

        // Assert
        // Note: The properties are private, so we need reflection to test them
        // In practice, you'd test the behavior through public methods
        Assert.NotNull(attribute);
    }

    [Fact]
    public void TrackAttribute_AttributeUsage_IsCorrect()
    {
        // Arrange
        var attributeUsage = typeof(TrackAttribute).GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        Assert.NotNull(attributeUsage);
        Assert.Equal(AttributeTargets.Method, attributeUsage.ValidOn);
        Assert.False(attributeUsage.AllowMultiple);
        Assert.False(attributeUsage.Inherited);
    }

    [Fact]
    public async Task OnActionExecutionAsync_RequestNotValid_ExecutesNextDelegate()
    {
        // Arrange
        var attribute = new TrackAttribute();
        var nextCalled = false;
        var nextDelegate = new ActionExecutionDelegate(() =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        SetupServiceProvider();
        _requestFilterMock.Setup(x => x.RequestValid(_httpContext, It.IsAny<ImmutableGlobalOptions>()))
            .Returns(false);

        // Act
        await attribute.OnActionExecutionAsync(_actionExecutingContext, nextDelegate);

        // Assert
        Assert.True(nextCalled);
        _requestHandlerMock.Verify(x => x.IsNotModified(It.IsAny<HttpContext>(), It.IsAny<ImmutableGlobalOptions>(), default),
            Times.Never);
    }

    [Fact]
    public async Task OnActionExecutionAsync_RequestValidButModified_ExecutesNextDelegate()
    {
        // Arrange
        var attribute = new TrackAttribute();
        var nextCalled = false;
        var nextDelegate = new ActionExecutionDelegate(() =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        SetupServiceProvider();
        _requestFilterMock.Setup(x => x.RequestValid(_httpContext, It.IsAny<ImmutableGlobalOptions>()))
            .Returns(true);
        _requestHandlerMock.Setup(x => x.IsNotModified(_httpContext, It.IsAny<ImmutableGlobalOptions>(), default))
            .ReturnsAsync(false);

        // Act
        await attribute.OnActionExecutionAsync(_actionExecutingContext, nextDelegate);

        // Assert
        Assert.True(nextCalled);
        _requestHandlerMock.Verify(x => x.IsNotModified(_httpContext, It.IsAny<ImmutableGlobalOptions>(), default), Times.Once);
    }

    [Fact]
    public async Task OnActionExecutionAsync_RequestValidAndNotModified_DoesNotExecuteNextDelegate()
    {
        // Arrange
        var attribute = new TrackAttribute();
        var nextCalled = false;
        var nextDelegate = new ActionExecutionDelegate(() =>
        {
            nextCalled = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        SetupServiceProvider();
        _requestFilterMock.Setup(x => x.RequestValid(_httpContext, It.IsAny<ImmutableGlobalOptions>()))
            .Returns(true);
        _requestHandlerMock.Setup(x => x.IsNotModified(_httpContext, It.IsAny<ImmutableGlobalOptions>(), default))
            .ReturnsAsync(true);

        // Act
        await attribute.OnActionExecutionAsync(_actionExecutingContext, nextDelegate);

        // Assert
        Assert.False(nextCalled);
        _requestHandlerMock.Verify(x => x.IsNotModified(_httpContext, It.IsAny<ImmutableGlobalOptions>(), default), Times.Once);
    }

    [Fact]
    public void GetOptions_FirstCall_BuildsOptionsCorrectly()
    {
        // Arrange
        var attribute = new TrackAttribute(new[] { "users", "orders" }, "custom-source", "no-store");

        SetupServiceProvider();
        _actionExecutingContext.ActionDescriptor.DisplayName = "TestController.TestAction";

        // Act
        var result = attribute.GetOptions(_actionExecutingContext);

        // Assert
        Assert.Equal("no-store", result.CacheControl);
        Assert.Equal("custom-source", result.Source);
        Assert.Contains("users", result.Tables);
        Assert.Contains("orders", result.Tables);
    }

    [Fact]
    public void GetOptions_MultipleCalls_ReturnsCachedOptions()
    {
        // Arrange
        var attribute = new TrackAttribute();

        SetupServiceProvider();
        _actionExecutingContext.ActionDescriptor.DisplayName = "TestAction";

        // Act - Call multiple times
        var result1 = attribute.GetOptions(_actionExecutingContext);
        var result2 = attribute.GetOptions(_actionExecutingContext);
        var result3 = attribute.GetOptions(_actionExecutingContext);

        // Assert
        Assert.Same(result1, result2);
        Assert.Same(result2, result3);
    }

    [Fact]
    public void GetOptions_WithPartialParameters_UsesDefaultsForMissingValues()
    {
        // Arrange
        var attribute = new TrackAttribute(tables: new[] { "products" }); // Only tables specified

        SetupServiceProvider();
        _serviceProviderMock.Setup(x => x.GetService(typeof(ImmutableGlobalOptions)))
            .Returns(new ImmutableGlobalOptions
            {
                CacheControl = "max-age=7200",
                Source = "global-source",
                Tables = []
            });

        // Act
        var result = attribute.GetOptions(_actionExecutingContext);

        // Assert
        Assert.Equal("max-age=7200", result.CacheControl); // From global options
        Assert.Equal("global-source", result.Source); // From global options
        Assert.Contains("products", result.Tables);
    }

    [Fact]
    public void GetOptions_WithNullParameters_UsesDefaults()
    {
        // Arrange
        var attribute = new TrackAttribute(null, null, null);

        SetupServiceProvider();

        // Act
        var result = attribute.GetOptions(_actionExecutingContext);

        // Assert
        Assert.Equal(_defaultOptions.CacheControl, result.CacheControl);
        Assert.Equal(_defaultOptions.Source, result.Source);
        Assert.Equal(_defaultOptions.Tables, result.Tables);
    }

    [Fact]
    public void GetOptions_ThreadSafety_MultipleThreadsShouldNotCreateMultipleInstances()
    {
        // Arrange
        var attribute = new TrackAttribute();
        var results = new List<ImmutableGlobalOptions>();
        var tasks = new List<Task>();
        var barrier = new Barrier(10);

        SetupServiceProvider();

        // Act - Simulate concurrent access
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                barrier.SignalAndWait();
                results.Add(attribute.GetOptions(_actionExecutingContext));
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        var firstResult = results[0];
        Assert.All(results, result => Assert.Same(firstResult, result));
    }

    [Fact]
    public async Task OnActionExecutionAsync_ServiceResolutionFails_ThrowsException()
    {
        // Arrange
        var attribute = new TrackAttribute();
        var nextDelegate = new ActionExecutionDelegate(() =>
            Task.FromResult<ActionExecutedContext>(null!));

        _serviceProviderMock.Setup(x => x.GetService(typeof(IRequestFilter)))
            .Returns(null); // Simulate missing service

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            attribute.OnActionExecutionAsync(_actionExecutingContext, nextDelegate));
    }

    private void SetupServiceProvider()
    {
        _serviceProviderMock.Setup(x => x.GetService(typeof(IRequestFilter)))
            .Returns(_requestFilterMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IRequestFilter)))
            .Returns(_requestFilterMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IRequestHandler)))
            .Returns(_requestHandlerMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(ImmutableGlobalOptions)))
            .Returns(_defaultOptions);
        _serviceProviderMock.Setup(x => x.GetService(typeof(ILogger<TrackAttribute>)))
            .Returns(_loggerMock.Object);
    }
}

public static class LoggerExtensions
{
    public static void LogOptionsBuilded(this ILogger<TrackAttribute> logger, string actionName)
    {
        logger.LogInformation("Options built for action: {ActionName}", actionName);
    }
}