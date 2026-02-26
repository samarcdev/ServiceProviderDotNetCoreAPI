using System;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Application.DTOs.Requests;

public class BookingAssignmentRequest
{
    [Required(ErrorMessage = "Booking ID is required")]
    public Guid BookingId { get; set; }
    
    public Guid? ServiceProviderId { get; set; }
    
    [StringLength(1000, ErrorMessage = "Admin notes cannot exceed 1000 characters")]
    public string? AdminNotes { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Estimated price must be non-negative")]
    public decimal? EstimatedPrice { get; set; }
    
    [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
    public string? Status { get; set; }
}
