namespace Tracker.AspNet.Services.Contracts;

public interface IETagService
{
    string AssemblyBuildTimeTicks { get; }
    int ComputeLength(ulong lastTimestamp, string suffix);
    bool EqualsTo(string ifNoneMatch, ulong lastTimestamp, string suffix);
    string Build(ulong lastTimestamp, string suffix);
}
