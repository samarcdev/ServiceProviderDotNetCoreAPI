using APIServiceManagement.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace APIServiceManagement.API.Attributes;

/// <summary>
/// Authorization attribute that requires Admin, MasterAdmin, or DefaultAdmin role
/// </summary>
public class AuthorizeAdminAttribute : AuthorizeAttribute
{
    public AuthorizeAdminAttribute()
    {
        Roles = $"{RoleNames.Admin},{RoleNames.MasterAdmin},{RoleNames.DefaultAdmin}";
    }
}
