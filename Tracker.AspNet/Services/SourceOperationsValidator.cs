using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Services;

public sealed class SourceOperationsValidator(IEnumerable<ISourceOperations> operations) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        ValidateSourceOperationsProviders();
        return next;
    }

    private void ValidateSourceOperationsProviders()
    {
        if (!operations.Any())
            throw new InvalidOperationException(
                $"At least one {nameof(ISourceOperations)} implementation is required");

        var duplicates = operations
            .GroupBy(o => o.SourceId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count != 0)
            throw new InvalidOperationException(
                $"Duplicate {nameof(ISourceOperations.SourceId)} values found: {string.Join(", ", duplicates)}");
    }
}
