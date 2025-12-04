using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet;

public sealed class SourceOperationsValidator(IEnumerable<ISourceOperations> operations) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        ValidateDuplicates();
        return next;
    }

    private void ValidateDuplicates()
    {
        var duplicates = operations
            .GroupBy(o => o.SourceId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count != 0)
            throw new InvalidOperationException(
                $"Duplicate SourceId values found: {string.Join(", ", duplicates)}");
    }
}
