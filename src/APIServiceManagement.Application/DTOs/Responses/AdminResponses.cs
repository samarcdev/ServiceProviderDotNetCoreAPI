using System;
using System.Collections.Generic;

namespace APIServiceManagement.Application.DTOs.Responses;

public class AdminDashboardStatsResponse
{
    public int PendingRequests { get; set; }
    public int VerifiedRequests { get; set; }
    public int RejectedRequests { get; set; }
    public int TotalRequests { get; set; }
    public int PendingBookings { get; set; }
    public int AssignedBookings { get; set; }
    public int InProgressBookings { get; set; }
    public int CompletedBookings { get; set; }
}

public class ServiceProviderVerificationResponse
{
    public int Id { get; set; }
    public Guid ProviderUserId { get; set; }
    public Guid? AssignedAdminId { get; set; }
    public string VerificationStatus { get; set; } = string.Empty; // "pending", "approved", "rejected", "under_review"
    public string? VerificationNotes { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Provider details
    public string? ProviderName { get; set; }
    public string? ProviderEmail { get; set; }
    public string? ProviderPhone { get; set; }
    public string? ProviderAddress { get; set; }
    public string? ProviderCity { get; set; }
    public string? ProviderState { get; set; }
    
    // Documents
    public List<VerificationDocumentResponse> Documents { get; set; } = new();
}

public class VerificationDocumentResponse
{
    public int Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class AdminPermissionResponse
{
    public bool IsAdmin { get; set; }
    public bool IsSuperAdmin { get; set; }
    public string? RoleName { get; set; }
}

public class AdminAssignedStateResponse
{
    public int Id { get; set; }
    public int StateId { get; set; }
    public string StateName { get; set; } = string.Empty;
}

public class ServiceProviderVerificationsWithBatchCountsResponse
{
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public List<ServiceProviderVerificationResponse> Verifications { get; set; } = new();
}