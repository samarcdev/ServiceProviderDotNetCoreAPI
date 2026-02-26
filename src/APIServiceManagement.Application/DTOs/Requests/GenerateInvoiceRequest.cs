using System;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Application.DTOs.Requests;

public class GenerateInvoiceRequest
{
    [Required(ErrorMessage = "Booking ID is required")]
    public Guid BookingId { get; set; }
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }
}
