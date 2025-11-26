namespace Npgsql.EFCore.Tracker.Api.Demo.Database.Entities;

public sealed class Role
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
