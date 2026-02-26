using System;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Application.DTOs.Requests;

public class AdminRescheduleRequest
{
    [Required(ErrorMessage = "Booking ID is required")]
    public Guid BookingId { get; set; }

    [Required(ErrorMessage = "Reschedule reason is required")]
    [StringLength(1000, MinimumLength = 5, ErrorMessage = "Reschedule reason must be between 5 and 1000 characters")]
    public string Reason { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Admin notes cannot exceed 1000 characters")]
    public string? AdminNotes { get; set; }

    public DateTime? SuggestedDate { get; set; }

    [StringLength(50, ErrorMessage = "Suggested time slot cannot exceed 50 characters")]
    public string? SuggestedTimeSlot { get; set; }
}
