using System;

namespace APIServiceManagement.Domain.Entities;

public class BookingStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public string Status { get; set; }
    public Guid? ChangedBy { get; set; }
    public string Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public BookingRequest Booking { get; set; }
    public User ChangedByUser { get; set; }
}