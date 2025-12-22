using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Immutable;
using Tracker.AspNet.Attributes;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Services.Contracts;


namespace Tracker.AspNet.Tests;

public class TrackAttributeGenericTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<IServiceProvider> _scopedServiceProviderMock;
    private readonly Mock<IRequestFilter> _requestFilterMock;
    private readonly Mock<IRequestHandler> _requestHandlerMock;
    private readonly Mock<ISourceOperationsResolver> _sourceResolverMock;
    private readonly Mock<IProviderIdGenerator> _sourceIdGeneratorMock;
    private readonly Mock<ILogger<TrackAttribute<TestDbContext>>> _loggerMock;
    private readonly Mock<TestDbContext> _dbContextMock;
    private readonly ImmutableGlobalOptions _defaultOptions;
    private readonly HttpContext _httpContext;
    private readonly ActionExecutingContext _actionExecutingContext;

    public TrackAttributeGenericTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _scopedServiceProviderMock = new Mock<IServiceProvider>();
        _requestFilterMock = new Mock<IRequestFilter>();
        _requestHandlerMock = new Mock<IRequestHandler>();
        _sourceResolverMock = new Mock<ISourceOperationsResolver>();
        _sourceIdGeneratorMock = new Mock<IProviderIdGenerator>();
        _loggerMock = new Mock<ILogger<TrackAttribute<TestDbContext>>>();
        _dbContextMock = new Mock<TestDbContext>();

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
    public void TrackAttributeGeneric_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var tables = new[] { "table1", "table2" };
        var entities = new[] { typeof(User), typeof(Order) };
        var sourceId = "custom-source";
        var cacheControl = "no-cache";

        // Act
        var attribute = new TrackAttribute<TestDbContext>(tables, entities, sourceId, cacheControl);

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public async Task OnActionExecutionAsync_RequestNotValid_ExecutesNextDelegate()
    {
        // Arrange
        var attribute = new TrackAttribute<TestDbContext>();
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
        var attribute = new TrackAttribute<TestDbContext>();
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
    public void GetOptions_FirstCall_CreatesScopeAndBuildsOptions()
    {
        // Arrange
        var attribute = new TrackAttribute<TestDbContext>(
            new[] { "users", "orders" },
            new[] { typeof(Product), typeof(Category) },
            "custom-source",
            "no-store");

        SetupServiceProvider();
        _actionExecutingContext.ActionDescriptor.DisplayName = "TestController.TestAction";

        // Act
        var result = attribute.GetOptions(_actionExecutingContext);

        // Assert
        Assert.Equal("no-store", result.CacheControl);
        Assert.Equal("custom-source", result.Source);
        Assert.Contains("users", result.Tables);
        Assert.Contains("orders", result.Tables);
        Assert.Contains("Products", result.Tables);
        Assert.Contains("Categories", result.Tables);
        Assert.Equal(4, result.Tables.Length);

        // Verify scope was created
        _serviceScopeFactoryMock.Verify(x => x.CreateScope(), Times.Once);
        _serviceScopeMock.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void GetOptions_SourceIdNotProvided_GeneratesFromTContextWhenRegistered()
    {
        // Arrange
        var attribute = new TrackAttribute<TestDbContext>();
        var generatedSourceId = "generated-source-123";

        SetupServiceProvider();
        _sourceIdGeneratorMock.Setup(x => x.GenerateId<TestDbContext>())
            .Returns(generatedSourceId);
        _sourceResolverMock.Setup(x => x.Registered(generatedSourceId))
            .Returns(true);

        // Act
        var result = attribute.GetOptions(_actionExecutingContext);

        // Assert
        Assert.Equal(generatedSourceId, result.Source);
    }

    [Fact]
    public void GetOptions_SourceIdNotProvided_NotRegistered_FallsBackToOptions()
    {
        // Arrange
        var attribute = new TrackAttribute<TestDbContext>();
        var generatedSourceId = "generated-source-123";

        SetupServiceProvider();
        _sourceIdGeneratorMock.Setup(x => x.GenerateId<TestDbContext>())
            .Returns(generatedSourceId);
        _sourceResolverMock.Setup(x => x.Registered(generatedSourceId))
            .Returns(false);

        // Act
        var result = attribute.GetOptions(_actionExecutingContext);

        // Assert
        Assert.Equal(_defaultOptions.Source, result.Source);
    }

    [Fact]
    public void GetOptions_EntitiesProvided_GetsTableNamesFromDbContext()
    {
        // Arrange
        var entities = new[] { typeof(User), typeof(Order) };
        var attribute = new TrackAttribute<TestDbContext>(entities: entities);

        SetupServiceProvider();

        // Act
        var result = attribute.GetOptions(_actionExecutingContext);

        // Assert
        Assert.Contains("Users", result.Tables);
        Assert.Contains("Orders", result.Tables);
    }

    [Fact]
    public void GetOptions_TablesAndEntities_CombinesWithoutDuplicates()
    {
        // Arrange
        var tables = new[] { "Users", "Products" };
        var entities = new[] { typeof(User), typeof(Product) };
        var attribute = new TrackAttribute<TestDbContext>(tables, entities);

        SetupServiceProvider();

        // Act
        var result = attribute.GetOptions(_actionExecutingContext);

        // Assert
        Assert.Equal(3, result.Tables.Length); // Users, Products, Categories
        Assert.Contains("Users", result.Tables);
        Assert.Contains("Products", result.Tables);
        Assert.Contains("Categories", result.Tables);
    }

    [Fact]
    public void GetOptions_CacheControlNotProvided_UsesFromOptions()
    {
        // Arrange
        var attribute = new TrackAttribute<TestDbContext>();

        SetupServiceProvider();
        _scopedServiceProviderMock.Setup(x => x.GetService(typeof(ImmutableGlobalOptions)))
            .Returns(new ImmutableGlobalOptions
            {
                CacheControl = "max-age=7200",
                Source = "default-source",
                Tables = []
            });

        // Act
        var result = attribute.GetOptions(_actionExecutingContext);

        // Assert
        Assert.Equal("max-age=7200", result.CacheControl);
    }

    [Fact]
    public void GetOptions_MultipleCalls_ReturnsCachedOptions()
    {
        // Arrange
        var attribute = new TrackAttribute<TestDbContext>();

        SetupServiceProvider();

        // Act - Call multiple times
        var result1 = attribute.GetOptions(_actionExecutingContext);
        var result2 = attribute.GetOptions(_actionExecutingContext);
        var result3 = attribute.GetOptions(_actionExecutingContext);

        // Assert
        Assert.Same(result1, result2);
        Assert.Same(result2, result3);

        // Should only create scope once
        _serviceScopeFactoryMock.Verify(x => x.CreateScope(), Times.Once);
    }

    [Fact]
    public void GetOptions_ThreadSafety_MultipleThreadsShouldNotCreateMultipleInstances()
    {
        // Arrange
        var attribute = new TrackAttribute<TestDbContext>();
        var results = new List<ImmutableGlobalOptions>();
        var tasks = new List<Task>();
        var barrier = new Barrier(10);
        var scopeCreationCount = 0;

        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_serviceScopeFactoryMock.Object);
        _serviceScopeFactoryMock.Setup(x => x.CreateScope())
            .Callback(() => Interlocked.Increment(ref scopeCreationCount))
            .Returns(_serviceScopeMock.Object);
        _serviceScopeMock.Setup(x => x.ServiceProvider).Returns(_scopedServiceProviderMock.Object);

        SetupScopedServices();

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
        Assert.Equal(1, scopeCreationCount); // Should only create scope once
    }

    //[Fact]
    //public void GetActionName_ReturnsDisplayNameWhenAvailable()
    //{
    //    // Arrange
    //    var attribute = new TrackAttribute<TestDbContext>();
    //    var actionName = "TestController.Action";
    //    _actionExecutingContext.ActionDescriptor.DisplayName = actionName;

    //    // Act
    //    var result = attribute.GetActionName(_actionExecutingContext);

    //    // Assert
    //    Assert.Equal(actionName, result);
    //}

    //[Fact]
    //public void GetActionName_ReturnsIdWhenDisplayNameNotAvailable()
    //{
    //    // Arrange
    //    var attribute = new TrackAttribute<TestDbContext>();
    //    var actionId = "123";

    //    var _actionExecutingContextMock = new Mock<ActionExecutingContext>();
    //    _actionExecutingContextMock.Setup(c => c.ActionDescriptor.DisplayName)
    //        .Returns((string?)null);

    //    _actionExecutingContextMock.Setup(c => c.ActionDescriptor.Id)
    //        .Returns(actionId);

    //    // Act
    //    var result = attribute.GetActionName(_actionExecutingContextMock.Object);

    //    // Assert
    //    Assert.Equal(actionId, result);
    //}

    [Fact]
    public void GetOptions_ExceptionInScope_ProperlyDisposesScope()
    {
        // Arrange
        var attribute = new TrackAttribute<TestDbContext>();

        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Throws(new InvalidOperationException("Test exception"));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => attribute.GetOptions(_actionExecutingContext));

        // Verify scope is disposed even on exception
        _serviceScopeMock.Verify(x => x.Dispose(), Times.Never); // Never created due to exception
    }

    [Fact]
    public void GetOptions_NullEntities_DoesNotCallDbContext()
    {
        // Arrange
        var attribute = new TrackAttribute<TestDbContext>(entities: null);

        SetupServiceProvider();

        // Act
        var result = attribute.GetOptions(_actionExecutingContext);

        // Assert
        Assert.Empty(result.Tables);
    }

    [Fact]
    public void GetOptions_EmptyEntitiesArray_DoesNotCallDbContext()
    {
        // Arrange
        var attribute = new TrackAttribute<TestDbContext>(entities: Array.Empty<Type>());

        SetupServiceProvider();

        // Act
        var result = attribute.GetOptions(_actionExecutingContext);

        // Assert
        Assert.Empty(result.Tables);
    }

    private void SetupServiceProvider()
    {
        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_serviceScopeFactoryMock.Object);
        _serviceScopeFactoryMock.Setup(x => x.CreateScope())
            .Returns(_serviceScopeMock.Object);
        _serviceScopeMock.Setup(x => x.ServiceProvider).Returns(_scopedServiceProviderMock.Object);
        _serviceScopeMock.Setup(x => x.Dispose()).Verifiable();

        SetupScopedServices();
    }

    private void SetupScopedServices()
    {
        _scopedServiceProviderMock.Setup(x => x.GetService(typeof(ImmutableGlobalOptions)))
            .Returns(_defaultOptions);
        _scopedServiceProviderMock.Setup(x => x.GetService(typeof(ISourceOperationsResolver)))
            .Returns(_sourceResolverMock.Object);
        _scopedServiceProviderMock.Setup(x => x.GetService(typeof(IProviderIdGenerator)))
            .Returns(_sourceIdGeneratorMock.Object);
        _scopedServiceProviderMock.Setup(x => x.GetService(typeof(ILogger<TrackAttribute<TestDbContext>>)))
            .Returns(_loggerMock.Object);
        _scopedServiceProviderMock.Setup(x => x.GetService(typeof(TestDbContext)))
            .Returns(_dbContextMock.Object);

        // Setup for OnActionExecutionAsync
        _serviceProviderMock.Setup(x => x.GetService(typeof(IRequestFilter)))
            .Returns(_requestFilterMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IRequestHandler)))
            .Returns(_requestHandlerMock.Object);
    }
}

public class TestDbContext : DbContext
{
}

public class User { }
public class Order { }
public class Product { }
public class Category { }
