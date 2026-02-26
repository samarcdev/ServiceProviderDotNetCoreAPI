using System;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Application.DTOs.Requests;

public class BookingUpdateRequest
{
    [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
    public string? Status { get; set; }
    
    [StringLength(1000, ErrorMessage = "Service provider notes cannot exceed 1000 characters")]
    public string? ServiceProviderNotes { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Final price must be non-negative")]
    public decimal? FinalPrice { get; set; }
    
    [Range(1, 5, ErrorMessage = "Customer rating must be between 1 and 5")]
    public int? CustomerRating { get; set; }
    
    [StringLength(1000, ErrorMessage = "Customer feedback cannot exceed 1000 characters")]
    public string? CustomerFeedback { get; set; }
    
    public DateTime? PreferredDate { get; set; }
    
    public TimeSpan? PreferredTime { get; set; }
    
    [Range(1, 24, ErrorMessage = "Working hours must be between 1 and 24")]
    public int? WorkingHours { get; set; }
    
    public DateTime? StartedAt { get; set; }
}
