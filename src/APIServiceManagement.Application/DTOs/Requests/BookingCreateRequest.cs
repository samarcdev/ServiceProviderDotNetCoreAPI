using System;

namespace APIServiceManagement.Application.DTOs.Requests;

public class BookingCreateRequest
{
    public int ServiceId { get; set; }
    public string Pincode { get; set; } = string.Empty;
    public string? RequestDescription { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerName { get; set; }
    public DateTime? PreferredDate { get; set; }
    public TimeSpan? PreferredTime { get; set; }
    public int? WorkingHours { get; set; }
}
