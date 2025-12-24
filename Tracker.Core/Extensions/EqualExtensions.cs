namespace Tracker.Core.Extensions;

internal static class EqualExtensions
{
    private static readonly int ULongMaxLength = ulong.MaxValue.ToString().Length;

    internal static bool EqualsULong(this ReadOnlySpan<char> chars, ulong number)
    {
        if (chars.Length == 0 || chars.Length > ULongMaxLength)
            return false;

        ulong result = 0;
        foreach (var c in chars)
        {
            if (c < '0' || c > '9') return false;
            result = result * 10 + (ulong)(c - '0');
        }

        return result == number;
    }
}
