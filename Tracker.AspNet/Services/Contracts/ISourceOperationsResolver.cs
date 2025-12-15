using System.Diagnostics.CodeAnalysis;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Services.Contracts;

public interface ISourceOperationsResolver
{
    ISourceOperations First { get; }

    bool Registered(string sourceId);

    bool TryResolve(string sourceId, [NotNullWhen(true)] out ISourceOperations? sourceOperations);
}
