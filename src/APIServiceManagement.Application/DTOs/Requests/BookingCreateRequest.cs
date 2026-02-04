using System;

namespace APIServiceManagement.Application.DTOs.Requests;

public class BookingCreateRequest
{
    public int ServiceId { get; set; }
    public int? ServiceTypeId { get; set; }
    public string Pincode { get; set; } = string.Empty;
    public string? RequestDescription { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? CustomerPhone { get; set; }
    public string? AlternativeMobileNumber { get; set; }
    public DateTime? PreferredDate { get; set; }
    public string? TimeSlot { get; set; } // e.g., "9-12", "12-3", "3-6"
    public int? WorkingHours { get; set; }
    public string? DiscountCode { get; set; }
    public decimal? BasePrice { get; set; }
    public decimal? LocationAdjustmentAmount { get; set; }
    public decimal? CalculatedPrice { get; set; }
    public int? DiscountId { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? PriceAfterDiscount { get; set; }
    public decimal? CgstAmount { get; set; }
    public decimal? SgstAmount { get; set; }
    public decimal? IgstAmount { get; set; }
    public decimal? TotalTaxAmount { get; set; }
    public decimal? ServiceChargeAmount { get; set; }
    public decimal? PlatformChargeAmount { get; set; }
    public decimal? FinalPrice { get; set; }
}
