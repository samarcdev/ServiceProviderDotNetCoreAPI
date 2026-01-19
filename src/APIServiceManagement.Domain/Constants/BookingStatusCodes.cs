namespace APIServiceManagement.Domain.Constants;

/// <summary>
/// Constants for booking status codes used in the database
/// </summary>
public static class BookingStatusCodes
{
    public const string Pending = "PENDING";
    public const string Assigned = "ASSIGNED";
    public const string InProgress = "IN_PROGRESS";
    public const string Completed = "COMPLETED";
    public const string Rejected = "REJECTED";
    public const string OnHold = "ON_HOLD";
    public const string Cancelled = "CANCELLED";
}
