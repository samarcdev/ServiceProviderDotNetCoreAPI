namespace APIServiceManagement.Domain.Constants;

/// <summary>
/// Constants for verification status strings used in API responses and requests
/// </summary>
public static class VerificationStatusStrings
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string UnderReview = "under_review";
}

/// <summary>
/// Constants for verification status codes used in database queries
/// </summary>
public static class VerificationStatusCodes
{
    public const string Pending = "PENDING";
    public const string Verified = "verified";
    public const string Approved = "approved";
}
