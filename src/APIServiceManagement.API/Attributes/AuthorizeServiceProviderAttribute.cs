using Microsoft.AspNetCore.Authorization;

namespace APIServiceManagement.API.Attributes;

/// <summary>
/// Authorization attribute that requires ServiceProvider role
/// </summary>
public class AuthorizeServiceProviderAttribute : AuthorizeAttribute
{
    public AuthorizeServiceProviderAttribute()
    {
        Roles = "ServiceProvider";
    }
}
