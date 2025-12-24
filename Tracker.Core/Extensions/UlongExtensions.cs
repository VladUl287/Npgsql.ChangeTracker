namespace Tracker.Core.Extensions;

internal static class UlongExtensions
{
    private const int ULongMaxLength = 20;

    internal static bool MatchesULong(this ReadOnlySpan<char> chars, ulong number)
    {
        if (chars is { Length: 0 or > ULongMaxLength })
            return false;

        ulong result = 0;
        foreach (var c in chars)
            result = result * 10 + (uint)(c - '0');

        return result == number;
    }

    internal static int CountDigits(this ulong number)
    {
        return number switch
        {
            < 10UL => 1,
            < 100UL => 2,
            < 1000UL => 3,
            < 10000UL => 4,
            < 100000UL => 5,
            < 1000000UL => 6,
            < 10000000UL => 7,
            < 100000000UL => 8,
            < 1000000000UL => 9,
            < 10000000000UL => 10,
            < 100000000000UL => 11,
            < 1000000000000UL => 12,
            < 10000000000000UL => 13,
            < 100000000000000UL => 14,
            < 1000000000000000UL => 15,
            < 10000000000000000UL => 16,
            < 100000000000000000UL => 17,
            < 1000000000000000000UL => 18,
            < 10000000000000000000UL => 19,
            _ => 20
        };
    }
}
