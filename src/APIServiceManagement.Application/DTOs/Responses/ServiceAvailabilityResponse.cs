using System.Collections.Generic;

namespace APIServiceManagement.Application.DTOs.Responses;

public class ServiceAvailabilityResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Pincode { get; set; }
    public bool AnyProviderAvailableInPincode { get; set; }
    public List<ServiceAvailabilityItem> Services { get; set; } = new();
}

public class ServiceAvailabilityItem
{
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string? Image { get; set; }
    public string? Icon { get; set; }
    public string? CategoryImage { get; set; }
    public string? CategoryIcon { get; set; }
    public string? CategoryName { get; set; }
    public bool IsAvailable { get; set; }
    public bool HasAvailableProvider { get; set; }
    public bool CanBook { get; set; }
    public decimal CalculatedPrice { get; set; }
    public decimal PriceRating { get; set; }
}
