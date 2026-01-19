namespace APIServiceManagement.Application.DTOs.Responses;

public class BookingSummaryResponse
{
    public bool Success { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int? ServiceTypeId { get; set; }
    public string? ServiceTypeName { get; set; }
    public string Pincode { get; set; } = string.Empty;
    public DateTime? PreferredDate { get; set; }
    public string? TimeSlot { get; set; }
    public decimal BasePrice { get; set; }
    public decimal CalculatedPrice { get; set; }
    public string? Message { get; set; }
}
