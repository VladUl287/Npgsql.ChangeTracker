using System.Reflection;

namespace Tracker.Core.Extensions;

public static class AssemblyExtensions
{
    public static DateTimeOffset GetAssemblyWriteTime(this Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly, nameof(assembly));

        if (string.IsNullOrEmpty(assembly.Location))
            throw new InvalidOperationException(
                $"Cannot determine write time for assembly '{assembly.FullName}' " +
                "because it has no physical location (may be dynamic or in-memory).");

        var assemblyPath = assembly.Location;

        if (!File.Exists(assemblyPath))
            throw new FileNotFoundException(
                $"Cannot determine write time for assembly '{assembly.FullName}'. " +
                $"Assembly file not found at '{assemblyPath}'");

        var lastWriteTimeUtc = File.GetLastWriteTimeUtc(assemblyPath);
        return new DateTimeOffset(lastWriteTimeUtc);
    }
}
