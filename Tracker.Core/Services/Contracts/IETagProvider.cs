namespace Tracker.Core.Services.Contracts;

public interface IETagProvider
{
    bool Compare(string etag, ulong lastTimestamp, string suffix);
    string Generate(ulong lastTimestamp, string suffix);
}
