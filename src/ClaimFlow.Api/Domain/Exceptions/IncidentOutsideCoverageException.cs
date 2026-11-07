namespace ClaimFlow.Api.Domain.Exceptions;

public class IncidentOutsideCoverageException(string policyNumber, DateOnly incidentDate)
    : DomainException($"Incident date {incidentDate:yyyy-MM-dd} for policy '{policyNumber}' falls outside its coverage period.");
