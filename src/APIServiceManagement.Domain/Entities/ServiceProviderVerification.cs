using System;

namespace APIServiceManagement.Domain.Entities;

public class ServiceProviderVerification
{
    public int Id { get; set; }
    public Guid ProviderUserId { get; set; }
    public Guid? AssignedAdminId { get; set; }
    public string VerificationStatus { get; set; } = "pending";
    public string VerificationNotes { get; set; }
    public string RejectionReason { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public User ProviderUser { get; set; }
    public User AssignedAdmin { get; set; }
    public User VerifiedByUser { get; set; }
}