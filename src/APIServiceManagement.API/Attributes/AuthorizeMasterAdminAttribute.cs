using APIServiceManagement.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace APIServiceManagement.API.Attributes;

/// <summary>
/// Authorization attribute that requires MasterAdmin role only
/// </summary>
public class AuthorizeMasterAdminAttribute : AuthorizeAttribute
{
    public AuthorizeMasterAdminAttribute()
    {
        Roles = RoleNames.MasterAdmin;
    }
}
