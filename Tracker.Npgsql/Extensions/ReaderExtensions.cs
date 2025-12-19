using Npgsql;
using System.Data.Common;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tracker.Npgsql.Tests")]

namespace Tracker.Npgsql.Extensions;

internal static class ReaderExtensions
{
    private const long PostgresTimestampOffsetTicks = 630822816000000000L;

    /// <summary>
    /// Reads a PostgreSQL timestamp value from the specified column and converts it to DateTime ticks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method converts a PostgreSQL timestamp (microseconds since 2000-01-01) to DateTime ticks
    /// (100-nanosecond intervals since 0001-01-01). The conversion applies a fixed offset to align the
    /// two timestamp systems.
    /// </para>
    /// <para>
    /// Adapted from <a href="https://github.com/npgsql/npgsql/blob/main/src/Npgsql/Internal/Converters/Temporal/PgTimestamp.cs">Npgsql source</a>.
    /// </para>
    /// </remarks>
    /// <param name="reader">The <see cref="DbDataReader"/> to read from.</param>
    /// <param name="ordinal">The zero-based column ordinal to read.</param>
    /// <returns>
    /// A <see cref="long"/> representing the timestamp as DateTime ticks.
    /// </returns>
    /// <exception cref="InvalidCastException">
    /// Thrown when the specified column does not contain a compatible timestamp value.
    /// </exception>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when the specified ordinal is out of range.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long GetTimestampTicks(this NpgsqlDataReader reader, int ordinal) =>
        reader.GetInt64(ordinal) * 10 + PostgresTimestampOffsetTicks;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long GetTimestampTicks(this DbDataReader reader, int ordinal) =>
        reader.GetInt64(ordinal) * 10 + PostgresTimestampOffsetTicks;
}
