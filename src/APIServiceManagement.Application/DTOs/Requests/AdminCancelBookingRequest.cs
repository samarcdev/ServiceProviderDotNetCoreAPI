using System;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Application.DTOs.Requests;

public class AdminCancelBookingRequest
{
    [Required(ErrorMessage = "Booking ID is required")]
    public Guid BookingId { get; set; }

    [Required(ErrorMessage = "Cancel reason is required")]
    [StringLength(1000, MinimumLength = 5, ErrorMessage = "Cancel reason must be between 5 and 1000 characters")]
    public string Reason { get; set; } = string.Empty;
}
