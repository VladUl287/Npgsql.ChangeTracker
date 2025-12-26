using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services;
using Tracker.AspNet.Utils;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Tests.ServicesTests;

public class DefaultOptionsBuilderTests
{
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ITableNameResolver> _mockTableNameResolver;
    private readonly DefaultOptionsBuilder _builder;

    public DefaultOptionsBuilderTests()
    {
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockTableNameResolver = new Mock<ITableNameResolver>();

        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);
        _mockScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockScope.Setup(x => x.Dispose());

        _builder = new DefaultOptionsBuilder(_mockScopeFactory.Object, _mockTableNameResolver.Object);
    }

    [Fact]
    public void Build_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        GlobalOptions options = null!;

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => _builder.Build(options));
    }

    [Fact]
    public void Build_ReturnsImmutableGlobalOptions_WithAllPropertiesSet()
    {
        // Arrange
        var options = new GlobalOptions
        {
            ProviderId = "TestProvider",
            Suffix = _ => "/suffix",
            Filter = _ => true,
            Tables = ["Table1", "Table2"],
            CacheControl = "max-age=3600",
            InvalidRequestDirectives = ["no-cache"],
            InvalidResponseDirectives = ["no-store"]
        };

        var sourceProviderMock = new Mock<ISourceProvider>();
        options.SourceProvider = sourceProviderMock.Object;
        options.SourceProviderFactory = _ => sourceProviderMock.Object;

        // Act
        var result = _builder.Build(options);

        // Assert
        Assert.Equal("TestProvider", result.ProviderId);
        Assert.Equal(sourceProviderMock.Object, result.SourceProvider);
        Assert.NotNull(result.SourceProviderFactory);
        Assert.Equal("max-age=3600", result.CacheControl);
        Assert.Equal("/suffix", result.Suffix(new DefaultHttpContext()));
        Assert.True(result.Filter(new DefaultHttpContext()));
        Assert.Equal(new[] { "Table1", "Table2" }, result.Tables);
        Assert.Equal(new[] { "no-cache" }, result.InvalidRequestDirectives);
        Assert.Equal(new[] { "no-store" }, result.InvalidResponseDirectives);
    }

    [Fact]
    public void Build_WithAllPropertiesSet_ReturnsCorrectImmutableOptions()
    {
        // Arrange
        var mockSourceProvider = new Mock<ISourceProvider>().Object;
        Func<HttpContext, ISourceProvider> sourceProviderFactory = ctx => mockSourceProvider;
        Func<HttpContext, bool> filter = ctx => false;

        static string suffix(HttpContext ctx) => "test-suffix";

        var options = new GlobalOptions
        {
            ProviderId = "test-provider",
            SourceProvider = mockSourceProvider,
            SourceProviderFactory = sourceProviderFactory,
            Filter = filter,
            InvalidRequestDirectives = ["directive1", "directive2"],
            InvalidResponseDirectives = ["response1", "response2"],
            Tables = ["Table1", "Table2"],
            CacheControl = "public, max-age=3600",
            Suffix = suffix
        };

        // Act
        var result = _builder.Build(options);

        // Assert
        Assert.Equal("test-provider", result.ProviderId);
        Assert.Same(mockSourceProvider, result.SourceProvider);
        Assert.Same(sourceProviderFactory, result.SourceProviderFactory);
        Assert.Same(filter, result.Filter);
        Assert.Equal("test-suffix", result.Suffix(Mock.Of<HttpContext>()));
        Assert.Equal("public, max-age=3600", result.CacheControl);

        Assert.Equal(2, result.Tables.Length);
        Assert.Contains("Table1", result.Tables);
        Assert.Contains("Table2", result.Tables);

        Assert.Equal(2, result.InvalidRequestDirectives.Length);
        Assert.Contains("directive1", result.InvalidRequestDirectives);
        Assert.Contains("directive2", result.InvalidRequestDirectives);

        Assert.Equal(2, result.InvalidResponseDirectives.Length);
        Assert.Contains("response1", result.InvalidResponseDirectives);
        Assert.Contains("response2", result.InvalidResponseDirectives);
    }

    [Fact]
    public void Build_WithNullCollections_ReturnsEmptyImmutableArrays()
    {
        // Arrange
        var options = new GlobalOptions
        {
            Tables = null!,
            InvalidRequestDirectives = null!,
            InvalidResponseDirectives = null!
        };

        // Act
        var result = _builder.Build(options);

        // Assert
        Assert.Empty(result.Tables);
        Assert.Empty(result.InvalidRequestDirectives);
        Assert.Empty(result.InvalidResponseDirectives);
    }

    [Theory]
    [InlineData(null, null, "no-cache")] // Default cache control
    [InlineData("custom-cache-control", null, "custom-cache-control")] // Custom cache control
    [InlineData(null, "builder-cache-control", "builder-cache-control")] // From builder
    public void Build_CacheControlResolution_ReturnsCorrectValue(
        string? cacheControl,
        string? builderCacheControl,
        string? expected)
    {
        // Arrange
        CacheControlBuilder? mockCacheControlBuilder = null;

        if (builderCacheControl is not null)
        {
            mockCacheControlBuilder = new CacheControlBuilder();
            mockCacheControlBuilder.WithDirective(builderCacheControl);
        }

        var options = new GlobalOptions
        {
            CacheControl = cacheControl,
            CacheControlBuilder = mockCacheControlBuilder
        };

        // Act
        var result = _builder.Build(options);

        // Assert
        Assert.Equal(expected, result.CacheControl);
    }

    [Fact]
    public void BuildGeneric_WithDbContext_ReturnsCombinedTables()
    {
        // Arrange
        var mockDbContext = new Mock<TestDbContext>();
        var entities = new[] { typeof(TestEntity1), typeof(TestEntity2) };
        var resolvedTables = new HashSet<string> { "ResolvedTable1", "ResolvedTable2" };

        _mockServiceProvider.Setup(x => x.GetService(typeof(TestDbContext)))
            .Returns(mockDbContext.Object);

        _mockTableNameResolver.Setup(x => x.GetTablesNames(mockDbContext.Object, entities))
            .Returns(resolvedTables);

        var options = new GlobalOptions
        {
            Tables = ["OptionTable1", "OptionTable2"],
            Entities = entities
        };

        // Act
        var result = _builder.Build<TestDbContext>(options);

        // Assert
        Assert.Equal(4, result.Tables.Length);
        Assert.Contains("OptionTable1", result.Tables);
        Assert.Contains("OptionTable2", result.Tables);
        Assert.Contains("ResolvedTable1", result.Tables);
        Assert.Contains("ResolvedTable2", result.Tables);

        _mockTableNameResolver.Verify(x => x.GetTablesNames(mockDbContext.Object, entities), Times.Once);
        _mockScope.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void BuildGeneric_WithNullEntities_ReturnsOnlyOptionTables()
    {
        // Arrange
        var mockDbContext = new Mock<TestDbContext>();

        _mockServiceProvider.Setup(x => x.GetService(typeof(TestDbContext)))
            .Returns(mockDbContext.Object);

        var options = new GlobalOptions
        {
            Tables = ["Table1", "Table2"],
            Entities = null
        };

        // Act
        var result = _builder.Build<TestDbContext>(options);

        // Assert
        Assert.Equal(2, result.Tables.Length);
        Assert.Contains("Table1", result.Tables);
        Assert.Contains("Table2", result.Tables);
    }

    [Fact]
    public void BuildGeneric_WithEmptyEntities_ReturnsOnlyOptionTables()
    {
        // Arrange
        var mockDbContext = new Mock<TestDbContext>();

        _mockServiceProvider.Setup(x => x.GetService(typeof(TestDbContext)))
            .Returns(mockDbContext.Object);

        var options = new GlobalOptions
        {
            Tables = ["Table1", "Table2"],
            Entities = []
        };

        // Act
        var result = _builder.Build<TestDbContext>(options);

        // Assert
        Assert.Equal(2, result.Tables.Length);
    }

    [Fact]
    public void BuildGeneric_WithNullTablesAndEntities_ReturnsEmptyTables()
    {
        // Arrange
        var mockDbContext = new Mock<TestDbContext>();

        _mockServiceProvider.Setup(x => x.GetService(typeof(TestDbContext)))
            .Returns(mockDbContext.Object);

        var options = new GlobalOptions
        {
            Tables = null,
            Entities = new[] { typeof(TestEntity1) }
        };

        var resolvedTables = new HashSet<string> { "ResolvedTable" };

        _mockTableNameResolver.Setup(x => x.GetTablesNames(mockDbContext.Object, options.Entities))
            .Returns(resolvedTables);

        // Act
        var result = _builder.Build<TestDbContext>(options);

        // Assert
        Assert.Single(result.Tables);
        Assert.Contains("ResolvedTable", result.Tables);
    }

    [Fact]
    public void BuildGeneric_ScopeIsDisposed_EvenOnException()
    {
        // Arrange
        var mockDbContext = new Mock<TestDbContext>();

        _mockServiceProvider.Setup(x => x.GetService(typeof(TestDbContext)))
            .Throws(new InvalidOperationException("Test exception"));

        var options = new GlobalOptions();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _builder.Build<TestDbContext>(options));

        _mockScope.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void BuildGeneric_AllOtherPropertiesArePreserved()
    {
        // Arrange
        var mockDbContext = new Mock<TestDbContext>();
        var mockSourceProvider = new Mock<ISourceProvider>().Object;

        _mockServiceProvider.Setup(x => x.GetService(typeof(TestDbContext)))
            .Returns(mockDbContext.Object);

        var options = new GlobalOptions
        {
            ProviderId = "test-provider",
            SourceProvider = mockSourceProvider,
            CacheControl = "test-cache",
            Tables = ["Table1"]
        };

        // Act
        var result = _builder.Build<TestDbContext>(options);

        // Assert
        Assert.Equal("test-provider", result.ProviderId);
        Assert.Same(mockSourceProvider, result.SourceProvider);
        Assert.Equal("test-cache", result.CacheControl);
        Assert.Single(result.Tables);
        Assert.Contains("Table1", result.Tables);
    }

    [Fact]
    public void BuildGeneric_DuplicateTables_AreRemoved()
    {
        // Arrange
        var mockDbContext = new Mock<TestDbContext>();
        var resolvedTables = new HashSet<string> { "Table1", "Table3" }; // Table1 is duplicate

        _mockServiceProvider.Setup(x => x.GetService(typeof(TestDbContext)))
            .Returns(mockDbContext.Object);

        _mockTableNameResolver.Setup(x => x.GetTablesNames(mockDbContext.Object, It.IsAny<Type[]>()))
            .Returns(resolvedTables);

        var options = new GlobalOptions
        {
            Tables = ["Table1", "Table2"],
            Entities = [typeof(TestEntity1)]
        };

        // Act
        var result = _builder.Build<TestDbContext>(options);

        // Assert
        Assert.Equal(3, result.Tables.Length);
        Assert.Contains("Table1", result.Tables);
        Assert.Contains("Table2", result.Tables);
        Assert.Contains("Table3", result.Tables);
    }

    public class TestDbContext : DbContext { }
    public class TestEntity1 { }
    public class TestEntity2 { }
}
