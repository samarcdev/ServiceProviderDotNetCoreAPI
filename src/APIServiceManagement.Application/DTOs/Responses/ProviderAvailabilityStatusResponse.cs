using System;

namespace APIServiceManagement.Application.DTOs.Responses;

public class ProviderAvailabilityStatusResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public bool IsCheckedIn { get; set; }
    public string? Pincode { get; set; }
    public DateTime? BusinessDate { get; set; }
    public DateTime? CheckInTimeUtc { get; set; }
    public DateTime? CheckOutTimeUtc { get; set; }
}
