namespace APIServiceManagement.Domain.Constants;

/// <summary>
/// Constants for booking status strings used for backward compatibility in the Status field
/// </summary>
public static class BookingStatusStrings
{
    public const string Pending = "pending";
    public const string Assigned = "assigned";
    public const string InProgress = "in_progress";
    public const string Completed = "completed";
    public const string Rejected = "rejected";
    public const string OnHold = "on_hold";
    public const string Cancelled = "cancelled";
    public const string RescheduleRequested = "reschedule_requested";
}
