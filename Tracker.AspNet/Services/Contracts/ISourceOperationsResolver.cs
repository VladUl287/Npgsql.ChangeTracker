using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Services.Contracts;

public interface ISourceOperationsResolver
{
    ISourceOperations First { get; }
    bool Registered(string sourceId);
    ISourceOperations? TryResolve(string? sourceId);
}
