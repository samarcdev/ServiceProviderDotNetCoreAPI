using System;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Domain.Entities;

public class BookingStatusHistory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ChangedBy { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; 

    // Navigation properties
    public BookingRequest? Booking { get; set; }
    public User? ChangedByUser { get; set; }
}