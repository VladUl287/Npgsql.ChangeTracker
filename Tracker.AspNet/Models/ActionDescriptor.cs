namespace Tracker.AspNet.Models;

public sealed class ActionDescriptor
{
    public string Route { get; init; } = string.Empty;
    public string[] Tables { get; init; } = [];
}
