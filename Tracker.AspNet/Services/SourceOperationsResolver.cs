using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Services;

public sealed class SourceOperationsResolver(IEnumerable<ISourceOperations> sourceOperations) : ISourceOperationsResolver
{
    private readonly FrozenDictionary<string, ISourceOperations> _store =
        sourceOperations.ToFrozenDictionary(c => c.SourceId);

    private readonly ISourceOperations _first =
        sourceOperations.First();

    public ISourceOperations First => _first;

    public bool Registered(string sourceId) => _first.SourceId == sourceId || _store.ContainsKey(sourceId);

    public bool TryResolve(string sourceId, [NotNullWhen(true)] out ISourceOperations? sourceOperations) => 
        _store.TryGetValue(sourceId, out sourceOperations);
}
