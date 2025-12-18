using System.Collections.Immutable;

namespace Tracker.Core.Services.Contracts;

/// <summary>
/// Defines operations for managing source data tracking and versions management.
/// </summary>
public interface ISourceOperations
{
    string SourceId { get; }

    ValueTask<long> GetLastVersion(string key, CancellationToken token = default);

    ValueTask GetLastVersions(ImmutableArray<string> keys, long[] versions, CancellationToken token = default);

    ValueTask<long> GetLastVersion(CancellationToken token = default);

    ValueTask<bool> EnableTracking(string key, CancellationToken token = default);

    ValueTask<bool> DisableTracking(string key, CancellationToken token = default);

    ValueTask<bool> IsTracking(string key, CancellationToken token = default);

    ValueTask<bool> SetLastVersion(string key, long value, CancellationToken token = default);
}
