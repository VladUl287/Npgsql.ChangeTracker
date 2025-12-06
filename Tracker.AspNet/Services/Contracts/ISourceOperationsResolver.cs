using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Services.Contracts;

public interface ISourceOperationsResolver
{
    ISourceOperations Resolve(string? sourceId);
}
