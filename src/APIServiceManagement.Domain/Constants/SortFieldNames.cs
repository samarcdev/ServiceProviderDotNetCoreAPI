namespace APIServiceManagement.Domain.Constants;

/// <summary>
/// Constants for sort field names used in API queries
/// </summary>
public static class SortFieldNames
{
    public const string Status = "status";
    public const string PreferredDate = "preferred_date";
    public const string CreatedAt = "created_at";
}

/// <summary>
/// Constants for sort order values
/// </summary>
public static class SortOrder
{
    public const string Ascending = "asc";
    public const string Descending = "desc";
}
