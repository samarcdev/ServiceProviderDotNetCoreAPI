# Role-Based Authorization Implementation

## Overview
This document describes the global role-based authorization system implemented in the API using .NET Core's built-in authorization features.

## Implementation Details

### 1. Custom Authorization Attributes
Created custom authorization attributes in `APIServiceManagement.API/Attributes/`:
- `AuthorizeAdminAttribute` - Requires Admin, MasterAdmin, or DefaultAdmin role
- `AuthorizeCustomerAttribute` - Requires Customer role
- `AuthorizeServiceProviderAttribute` - Requires ServiceProvider role
- `AuthorizeMasterAdminAttribute` - Requires MasterAdmin role only

### 2. Authorization Policies
Configured global authorization policies in `ApiConfiguration.cs`:
- **Admin Policy**: Allows Admin, MasterAdmin, or DefaultAdmin
- **MasterAdmin Policy**: Allows only MasterAdmin
- **Customer Policy**: Allows only Customer
- **ServiceProvider Policy**: Allows only ServiceProvider
- **ServiceProviderOrCustomer Policy**: Allows both ServiceProvider and Customer

### 3. JWT Token Configuration
Updated JWT Bearer authentication to properly map role claims:
- `RoleClaimType = ClaimTypes.Role` - Maps role claims from JWT token
- `NameClaimType = ClaimTypes.NameIdentifier` - Maps user ID claims

The JWT tokens already include role information in the `ClaimTypes.Role` claim (set in `AuthService.BuildAuthResponse`).

### 4. Controller Updates
All controllers have been updated to use role-based authorization:

#### AdminController
- Entire controller requires `[AuthorizeAdmin]` attribute

#### UsersController
- `GetAll()`, `Create()`, `Update()`, `Delete()` - Require `[AuthorizeAdmin]`
- `GetById()` - Requires only `[Authorize]` (any authenticated user)

#### DashboardController
- `GetCustomerDashboard()` - Requires `[AuthorizeCustomer]`
- `GetAdminDashboard()` - Requires `[AuthorizeAdmin]`
- `GetServiceProviderDashboard()` - Requires `[AuthorizeServiceProvider]`

#### BookingsController
- Customer-specific endpoints use `[AuthorizeCustomer]`:
  - `GetBookingSummary()`
  - `CreateBooking()`
  - `GetUserPincodePreferences()`
  - `SaveUserPincodePreference()`
  - `DeleteUserPincodePreference()`
  - `CancelBooking()`
- Admin-specific endpoints use `[AuthorizeAdmin]`:
  - `AssignServiceProvider()`
- General endpoints use `[Authorize]`:
  - `GetBookingRequests()` - Accessible by any authenticated user

#### ServiceProvidersController
- All service provider endpoints use `[AuthorizeServiceProvider]`:
  - `GetProfile()`
  - `UpdateProfile()`
  - `GetMissingDetails()`
  - `UpdateMissingDetails()`
  - `GetDashboard()`
  - `GetBookings()`
  - `UploadDocument()`
  - `UpdateServices()`

## How It Works

1. **Authentication**: User logs in and receives a JWT token containing role information in the `ClaimTypes.Role` claim.

2. **Authorization**: When a request is made:
   - The JWT token is validated by the authentication middleware
   - Role claims are extracted and mapped to `User.Claims`
   - The authorization middleware checks if the user has the required role(s)
   - If authorized, the request proceeds; otherwise, a 403 Forbidden response is returned

3. **Global Enforcement**: All role checks are handled at the middleware/attribute level, eliminating the need for manual role checking in service layers.

## Benefits

1. **Centralized Authorization**: All role checks are defined at the controller/action level using attributes
2. **Type Safety**: Custom attributes provide compile-time checking
3. **Maintainability**: Easy to update role requirements by changing attributes
4. **Security**: Authorization is enforced before reaching business logic
5. **Consistency**: All endpoints follow the same authorization pattern

## Role Names

The system uses the following role names (defined in `RoleNames` constants):
- `MasterAdmin`
- `Admin`
- `DefaultAdmin`
- `ServiceProvider`
- `Customer`

These role names must match the role names stored in the database `Roles` table.

## Testing

To verify authorization is working:
1. Test with different user roles and verify access is granted/denied correctly
2. Verify that unauthorized requests return 401 (Unauthorized) or 403 (Forbidden)
3. Check that JWT tokens contain the correct role claims
4. Ensure that role-based endpoints are properly protected

## Notes

- The existing service layer code that manually checks roles has been left intact for backward compatibility
- Authorization is now enforced at the API level, providing an additional layer of security
- Anonymous endpoints (marked with `[AllowAnonymous]`) remain accessible without authentication
