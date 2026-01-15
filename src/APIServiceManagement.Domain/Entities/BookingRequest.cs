using System;

namespace APIServiceManagement.Domain.Entities;

public class BookingRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public int ServiceId { get; set; }
    public string Pincode { get; set; }
    public Guid? ServiceProviderId { get; set; }
    public Guid? AdminId { get; set; }
    public string Status { get; set; } = "pending";
    public string RequestDescription { get; set; }
    public string CustomerAddress { get; set; }
    public string CustomerPhone { get; set; }
    public string CustomerName { get; set; }
    public DateTime? PreferredDate { get; set; }
    public TimeSpan? PreferredTime { get; set; }
    public decimal? EstimatedPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public string AdminNotes { get; set; }
    public string ServiceProviderNotes { get; set; }
    public int? CustomerRating { get; set; }
    public string CustomerFeedback { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int WorkingHours { get; set; } = 1; 
    // Navigation properties
    public User Customer { get; set; }
    public Service Service { get; set; }
    public User ServiceProvider { get; set; }
    public User Admin { get; set; }
    public ICollection<BookingStatusHistory> BookingStatusHistories { get; set; }
}