using System;
using System.Collections.Generic;

namespace APIServiceManagement.Application.DTOs.Responses;

public class MasterAdminDashboardStatsResponse
{
    public int TotalUsers { get; set; }
    public int TotalAdmins { get; set; }
    public int TotalServiceProviders { get; set; }
    public int TotalCustomers { get; set; }
    public int PendingVerifications { get; set; }
    public int TotalBookings { get; set; }
    public int PendingBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int TotalCategories { get; set; }
    public int TotalServices { get; set; }
    public int TotalStates { get; set; }
    public int TotalCities { get; set; }
}

public class UserManagementResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public string? VerificationStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminManagementResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public List<AdminAssignedStateResponse> AssignedStates { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class MasterDataCategoryResponse
{
    public int Id { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public bool IsActive { get; set; }
    public string? Image { get; set; }
    public string? Icon { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MasterDataServiceResponse
{
    public int Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Image { get; set; }
    public string? Icon { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MasterDataStateResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MasterDataCityResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StateId { get; set; }
    public string StateName { get; set; } = string.Empty;
    public string? Pincode { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MasterDataCityPincodeResponse
{
    public int Id { get; set; }
    public int CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public int StateId { get; set; }
    public string StateName { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAdminResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? AdminId { get; set; }
    public List<AlreadyAssignedStateInfo> AlreadyAssignedStates { get; set; } = new();
    public int? AssignedStateId { get; set; }
    public string? AssignedStateName { get; set; }
}

public class AlreadyAssignedStateInfo
{
    public int StateId { get; set; }
    public string StateName { get; set; } = string.Empty;
    public Guid AssignedToAdminId { get; set; }
    public string AssignedToAdminEmail { get; set; } = string.Empty;
}