using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("booking_status_histories")]
public class BookingStatusHistory
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Column("booking_id")]
    public Guid BookingId { get; set; }
    [Column("status_id")]
    public int StatusId { get; set; }
    [Column("status")]
    public string Status { get; set; } = string.Empty; // Kept for backward compatibility during migration
    [Column("changed_by")]
    public Guid? ChangedBy { get; set; }
    [Column("notes")]
    public string Notes { get; set; } = string.Empty;
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; 

    // Navigation properties
    public BookingRequest? Booking { get; set; }
    public User? ChangedByUser { get; set; }
    public BookingStatus? StatusNavigation { get; set; }
}