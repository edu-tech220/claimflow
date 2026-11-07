namespace ClaimFlow.Api.Data;

/// <summary>
/// Implemented by entities that use an application-managed integer as an optimistic
/// concurrency token instead of a provider-specific rowversion column, so the same
/// concurrency behavior works on both SQL Server and SQLite.
/// </summary>
public interface IHasConcurrencyVersion
{
    int Version { get; set; }
}
