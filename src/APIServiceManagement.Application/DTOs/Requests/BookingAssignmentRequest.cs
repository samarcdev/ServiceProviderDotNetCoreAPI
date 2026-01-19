using System;

namespace APIServiceManagement.Application.DTOs.Requests;

public class BookingAssignmentRequest
{
    public Guid BookingId { get; set; }
    public Guid? ServiceProviderId { get; set; }
    public string? AdminNotes { get; set; }
    public decimal? EstimatedPrice { get; set; }
    public string? Status { get; set; }
}
