namespace Tracker.Core.Services.Contracts;

public interface IETagService
{
    bool EqualsTo(string etag, ulong lastTimestamp, string suffix);
    string Build(ulong lastTimestamp, string suffix);
}
