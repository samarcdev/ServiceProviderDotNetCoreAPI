using System;

namespace APIServiceManagement.Application.DTOs.Requests;

public class GenerateInvoiceRequest
{
    public Guid BookingId { get; set; }
    public string? Notes { get; set; }
}
