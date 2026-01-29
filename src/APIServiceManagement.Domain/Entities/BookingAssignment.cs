using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("booking_assignments")]
public class BookingAssignment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("booking_id")]
    public Guid BookingRequestId { get; set; }

    [Column("service_provider_id")]
    public Guid ServiceProviderId { get; set; }

    [Column("assigned_by")]
    public Guid? AssignedByUserId { get; set; }

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    [Column("unassigned_at")]
    public DateTime? UnassignedAt { get; set; }

    [Column("unassigned_reason")]
    public string? UnassignedReason { get; set; }

    [Column("reason_type")]
    [MaxLength(50)]
    public string? ReasonType { get; set; } // 'leave', 'rejection', 'reassignment', 'other'

    [Column("is_current")]
    public bool IsCurrent { get; set; } = true;

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public BookingRequest BookingRequest { get; set; }
    public User ServiceProvider { get; set; }
    public User? AssignedByUser { get; set; }
}
