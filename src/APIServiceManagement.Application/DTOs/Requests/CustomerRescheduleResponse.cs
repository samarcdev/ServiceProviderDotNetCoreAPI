using System;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Application.DTOs.Requests;

public class CustomerRescheduleResponse
{
    [Required(ErrorMessage = "Booking ID is required")]
    public Guid BookingId { get; set; }

    /// <summary>
    /// If true, the customer is cancelling the booking instead of rescheduling.
    /// </summary>
    public bool CancelBooking { get; set; }

    [Required(ErrorMessage = "Preferred date is required when rescheduling")]
    public DateTime? PreferredDate { get; set; }

    [StringLength(50, ErrorMessage = "Preferred time slot cannot exceed 50 characters")]
    public string? TimeSlot { get; set; }

    [Range(1, 24, ErrorMessage = "Working hours must be between 1 and 24")]
    public int? WorkingHours { get; set; }
}
