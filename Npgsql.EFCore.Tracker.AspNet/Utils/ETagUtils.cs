using System.Security.Cryptography;
using System.Text;

namespace Npgsql.EFCore.Tracker.AspNet.Utils;

public static class ETagUtils
{
    public static string GenETagTicks(DateTimeOffset timestamp)
    {
        var ticks = timestamp.UtcTicks;
        return ticks.ToString("x");
    }

    public static string GenETagTicks(params DateTimeOffset[] timestamps)
    {
        long xorResult = 0;

        foreach (var timestamp in timestamps)
            xorResult ^= timestamp.UtcTicks;

        return xorResult.ToString("x16");
    }

    public static string GenETag(DateTimeOffset timestamp)
    {
        var utcTimestamp = timestamp.UtcDateTime;
        var timestampString = utcTimestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(timestampString);
        var hash = SHA256.HashData(bytes);

        return Convert.ToBase64String(hash);
    }

    public static string GenETag(params DateTimeOffset[] timestamps)
    {
        var sortedTimestamps = timestamps
            .OrderBy(t => t.UtcTicks)
            .Select(t => t.UtcDateTime.ToString("O")); // ISO 8601 format

        var combinedString = string.Join("|", sortedTimestamps);

        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(combinedString);
        var hash = SHA256.HashData(bytes);

        return Convert.ToBase64String(hash);
    }
}
