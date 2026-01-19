using System;

namespace APIServiceManagement.Application.DTOs.Requests;

public class BookingUpdateRequest
{
    public string? Status { get; set; }
    public string? ServiceProviderNotes { get; set; }
    public decimal? FinalPrice { get; set; }
    public int? CustomerRating { get; set; }
    public string? CustomerFeedback { get; set; }
    public DateTime? PreferredDate { get; set; }
    public TimeSpan? PreferredTime { get; set; }
    public int? WorkingHours { get; set; }
    public DateTime? StartedAt { get; set; }
}
