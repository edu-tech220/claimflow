namespace ClaimFlow.Api.Services;

public class ClaimRulesOptions
{
    public const string SectionName = "ClaimRules";

    /// <summary>
    /// Payable amounts above this threshold require a second, supervisor-level approval
    /// before they can be marked paid.
    /// </summary>
    public decimal SupervisorApprovalThreshold { get; set; } = 5000m;

    /// <summary>
    /// Claims left in UnderReview or PendingSupervisorApproval longer than this are
    /// flagged as escalated by the background SLA monitor.
    /// </summary>
    public int ReviewSlaHours { get; set; } = 72;

    /// <summary>
    /// How often the background SLA monitor sweeps for overdue claims.
    /// </summary>
    public int EscalationSweepIntervalMinutes { get; set; } = 60;
}
