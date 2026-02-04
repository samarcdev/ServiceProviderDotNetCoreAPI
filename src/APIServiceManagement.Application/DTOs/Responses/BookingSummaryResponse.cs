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
    public decimal LocationAdjustmentAmount { get; set; }
    public decimal CalculatedPrice { get; set; }
    public int? DiscountId { get; set; }
    public string? DiscountName { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PriceAfterDiscount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstAmount { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal ServiceChargeAmount { get; set; }
    public decimal PlatformChargeAmount { get; set; }
    public decimal FinalPrice { get; set; }
    public bool IsSameState { get; set; }
    public string? Message { get; set; }
}
