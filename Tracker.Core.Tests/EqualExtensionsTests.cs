using Tracker.Core.Extensions;

namespace Tracker.Core.Tests;

public class EqualExtensionsTests
{
    [Fact]
    public void Equal_Default()
    {
        // Arrange
        ulong a = 123;
        ReadOnlySpan<char> s = "123";

        // Act
        var equal = s.EqualsULong(a);

        // Assert
        Assert.True(equal);
    }

    [Fact]
    public void Equal_MaxValue()
    {
        // Arrange
        ulong a = ulong.MaxValue;
        ReadOnlySpan<char> s = ulong.MaxValue.ToString();

        // Act
        var equal = s.EqualsULong(a);

        // Assert
        Assert.True(equal);
    }

    [Fact]
    public void Equal_MinValue()
    {
        // Arrange
        ulong a = ulong.MinValue;
        ReadOnlySpan<char> s = ulong.MinValue.ToString();

        // Act
        var equal = s.EqualsULong(a);

        // Assert
        Assert.True(equal);
    }

    [Fact]
    public void Not_Equal_Default()
    {
        // Arrange
        ulong a = 123;
        ReadOnlySpan<char> s = "321";

        // Act
        var equal = s.EqualsULong(a);

        // Assert
        Assert.False(equal);
    }

    [Fact]
    public void Not_Equal_Special_Chars()
    {
        // Arrange
        ulong a = 123;
        ReadOnlySpan<char> s = "12$";

        // Act
        var equal = s.EqualsULong(a);

        // Assert
        Assert.False(equal);
    }

    [Fact]
    public void Not_Equal_Letters()
    {
        // Arrange
        ulong a = 123;
        ReadOnlySpan<char> s = "1a3";

        // Act
        var equal = s.EqualsULong(a);

        // Assert
        Assert.False(equal);
    }

    [Fact]
    public void Not_Equal_Empty_String()
    {
        // Arrange
        ulong a = 0;
        ReadOnlySpan<char> s = "";

        // Act
        var equal = s.EqualsULong(a);

        // Assert
        Assert.False(equal);
    }

    [Fact]
    public void Not_Equal_String_Default_And_Ulong_Default()
    {
        // Arrange
        ulong a = default;
        ReadOnlySpan<char> s = default;

        // Act
        var equal = s.EqualsULong(a);

        // Assert
        Assert.False(equal);
    }

    [Fact]
    public void Not_Equal_String_Overflow_ULong_Size()
    {
        // Arrange
        ulong a = default;
        ReadOnlySpan<char> s = "1234567891234567891234567891234456789";

        // Act
        var equal = s.EqualsULong(a);

        // Assert
        Assert.False(equal);
    }
}
