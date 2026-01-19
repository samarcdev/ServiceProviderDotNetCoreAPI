using APIServiceManagement.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace APIServiceManagement.API.Attributes;

/// <summary>
/// Authorization attribute that requires Customer role
/// </summary>
public class AuthorizeCustomerAttribute : AuthorizeAttribute
{
    public AuthorizeCustomerAttribute()
    {
        Roles = RoleNames.Customer;
    }
}
