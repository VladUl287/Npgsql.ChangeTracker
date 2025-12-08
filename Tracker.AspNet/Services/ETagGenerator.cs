using System.Reflection;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Extensions;

namespace Tracker.AspNet.Services;

public class ETagGenerator(Assembly executionAssembly) : IETagGenerator
{
    private readonly DateTimeOffset _assemblyBuildTime = executionAssembly.GetAssemblyWriteTime();
    private readonly string _assemblyBuildTimeTicks = executionAssembly.GetAssemblyWriteTime().Ticks.ToString();

    public string AssemblyBuildTimeTicks => _assemblyBuildTimeTicks;

    public string GenerateETag(DateTimeOffset timestamp, string suffix)
    {
        var etag = $"{_assemblyBuildTime.Ticks}-{timestamp.Ticks}";
        if (!string.IsNullOrEmpty(suffix))
            etag += $"-{suffix}";
        return etag;
    }

    public string GenerateETag(DateTimeOffset[] timestamps, string suffix)
    {
        long xorResult = 0;

        foreach (var timestamp in timestamps)
            xorResult ^= timestamp.UtcTicks;

        var x16 = xorResult.ToString("x16");
        var etag = $"{_assemblyBuildTime.Ticks}-{x16}";
        if (!string.IsNullOrEmpty(suffix))
            etag += $"-{suffix}";
        return etag;
    }
}
