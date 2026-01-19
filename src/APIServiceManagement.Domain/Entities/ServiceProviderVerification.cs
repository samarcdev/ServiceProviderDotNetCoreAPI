using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("service_provider_verifications")]
public class ServiceProviderVerification
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    [Column("provider_user_id")]
    public Guid ProviderUserId { get; set; }
    [Column("assigned_admin_id")]
    public Guid? AssignedAdminId { get; set; }
    [Column("verification_notes")]
    public string? VerificationNotes { get; set; }
    [Column("rejection_reason")]
    public string? RejectionReason { get; set; }
    [Column("verified_at")]
    public DateTime? VerifiedAt { get; set; }
    [Column("verified_by")]
    public Guid? VerifiedBy { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public User ProviderUser { get; set; }
    public User AssignedAdmin { get; set; }
    public User VerifiedByUser { get; set; }
}