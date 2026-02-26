using System;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Application.DTOs.Requests;

public class BookingCreateRequest
{
    [Required(ErrorMessage = "Service ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Service ID must be greater than 0")]
    public int ServiceId { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Service Type ID must be greater than 0")]
    public int? ServiceTypeId { get; set; }
    
    [Required(ErrorMessage = "Pincode is required")]
    [StringLength(10, MinimumLength = 6, ErrorMessage = "Pincode must be between 6 and 10 characters")]
    public string Pincode { get; set; } = string.Empty;
    
    [StringLength(1000, ErrorMessage = "Request description cannot exceed 1000 characters")]
    public string? RequestDescription { get; set; }
    
    [StringLength(200, ErrorMessage = "Address line 1 cannot exceed 200 characters")]
    public string? AddressLine1 { get; set; }
    
    [StringLength(200, ErrorMessage = "Address line 2 cannot exceed 200 characters")]
    public string? AddressLine2 { get; set; }
    
    [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
    public string? City { get; set; }
    
    [StringLength(100, ErrorMessage = "State cannot exceed 100 characters")]
    public string? State { get; set; }
    
    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, ErrorMessage = "Customer phone cannot exceed 20 characters")]
    public string? CustomerPhone { get; set; }
    
    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, ErrorMessage = "Alternative mobile number cannot exceed 20 characters")]
    public string? AlternativeMobileNumber { get; set; }
    
    public DateTime? PreferredDate { get; set; }
    
    [StringLength(20, ErrorMessage = "Time slot cannot exceed 20 characters")]
    public string? TimeSlot { get; set; } // e.g., "9-12", "12-3", "3-6"
    
    [Range(1, 24, ErrorMessage = "Working hours must be between 1 and 24")]
    public int? WorkingHours { get; set; }
    
    [StringLength(50, ErrorMessage = "Discount code cannot exceed 50 characters")]
    public string? DiscountCode { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Base price must be non-negative")]
    public decimal? BasePrice { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Location adjustment amount must be non-negative")]
    public decimal? LocationAdjustmentAmount { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Calculated price must be non-negative")]
    public decimal? CalculatedPrice { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Discount ID must be greater than 0")]
    public int? DiscountId { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Discount amount must be non-negative")]
    public decimal? DiscountAmount { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Price after discount must be non-negative")]
    public decimal? PriceAfterDiscount { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "CGST amount must be non-negative")]
    public decimal? CgstAmount { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "SGST amount must be non-negative")]
    public decimal? SgstAmount { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "IGST amount must be non-negative")]
    public decimal? IgstAmount { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Total tax amount must be non-negative")]
    public decimal? TotalTaxAmount { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Service charge amount must be non-negative")]
    public decimal? ServiceChargeAmount { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Platform charge amount must be non-negative")]
    public decimal? PlatformChargeAmount { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Final price must be non-negative")]
    public decimal? FinalPrice { get; set; }
}
