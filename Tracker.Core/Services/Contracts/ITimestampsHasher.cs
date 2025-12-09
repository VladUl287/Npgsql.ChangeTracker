namespace Tracker.Core.Services.Contracts;

public interface ITimestampsHasher
{
    ulong Hash(Span<DateTimeOffset> timestamps);
}
