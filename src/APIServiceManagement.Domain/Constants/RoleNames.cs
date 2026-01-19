namespace APIServiceManagement.Domain.Constants;

/// <summary>
/// Constants for role names used throughout the application
/// </summary>
public static class RoleNames
{
    public const string MasterAdmin = "MasterAdmin";
    public const string Admin = "Admin";
    public const string DefaultAdmin = "DefaultAdmin";
    public const string SuperAdmin = "SuperAdmin";
    public const string ServiceProvider = "ServiceProvider";
    public const string Customer = "Customer";
    
    /// <summary>
    /// Normalized role names (lowercase, no underscores, no hyphens) for comparison
    /// </summary>
    public static class Normalized
    {
        public const string Admin = "admin";
        public const string MasterAdmin = "masteradmin";
        public const string DefaultAdmin = "defaultadmin";
        public const string SuperAdmin = "superadmin";
    }
}
