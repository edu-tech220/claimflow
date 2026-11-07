namespace ClaimFlow.Api.Domain.Exceptions;

/// <summary>
/// Base type for business rule violations. The API layer maps these to RFC 7807
/// responses instead of generic 500s.
/// </summary>
public abstract class DomainException(string message) : Exception(message);
