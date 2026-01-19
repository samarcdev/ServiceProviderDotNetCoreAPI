using System;
using System.Collections.Generic;

namespace APIServiceManagement.Application.DTOs.Responses;

public class ServiceProviderProfileResponse
{
    public Guid UserId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? MobileNumber { get; set; } 
    public bool IsVerified { get; set; }
    public ServiceProviderExtraInfoResponse? ExtraInfo { get; set; }
    public ServiceProviderAddressResponse? Address { get; set; }
    public ServiceProviderProfileVerificationResponse? Verification { get; set; }
    public List<ServiceProviderDocumentResponse> Documents { get; set; } = new();
    public List<UserPincodePreferenceResponse> PincodePreferences { get; set; } = new();
    public List<ServiceProviderServiceResponse> Services { get; set; } = new();
    public string? Experience { get; set; }
    public bool HasExperience { get; set; }
}

public class ServiceProviderExtraInfoResponse
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AlternativeMobile { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string? StatusCode { get; set; }
    public string? StatusName { get; set; }
    public bool IsMobileVerified { get; set; }
    public bool IsAcceptedTerms { get; set; }
    public bool IsCompleted { get; set; }
}

public class ServiceProviderAddressResponse
{
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public int? CityId { get; set; }
    public string? CityName { get; set; }
    public int? StateId { get; set; }
    public string? StateName { get; set; }
}

public class ServiceProviderProfileVerificationResponse
{
    public int VerificationStatusId { get; set; }
    public string? VerificationStatusCode { get; set; }
    public string? VerificationStatusName { get; set; }
    public string VerificationNotes { get; set; } = string.Empty;
    public string RejectionReason { get; set; } = string.Empty;
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? VerifiedBy { get; set; }
    public string? VerifiedByName { get; set; }
    public Guid? AssignedAdminId { get; set; }
    public string? AssignedAdminName { get; set; }
}

public class ServiceProviderDocumentResponse
{
    public int DocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public bool IsActive { get; set; }
}

public class ServiceProviderServiceResponse
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string? Availability { get; set; }
}
