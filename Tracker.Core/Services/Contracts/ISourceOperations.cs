using System.Collections.Immutable;

namespace Tracker.Core.Services.Contracts;

public interface ISourceOperations
{
    string SourceId { get; }
    Task<DateTimeOffset> GetLastTimestamp(string key, CancellationToken token);
    Task GetLastTimestamps(ImmutableArray<string> keys, DateTimeOffset[] timestamps, CancellationToken token);
    Task<DateTimeOffset> GetLastTimestamp(CancellationToken token);
}
