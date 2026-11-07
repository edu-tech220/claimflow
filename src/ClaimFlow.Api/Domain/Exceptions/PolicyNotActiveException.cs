namespace ClaimFlow.Api.Domain.Exceptions;

public class PolicyNotActiveException(string policyNumber)
    : DomainException($"Policy '{policyNumber}' is not active and cannot accept new claims.");
