using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Application.DTOs.Requests;

public class CreateCreditNoteRequest
{
    [Required(ErrorMessage = "Invoice ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Invoice ID must be greater than 0")]
    public int InvoiceId { get; set; }
    
    [Required(ErrorMessage = "Credit type is required")]
    [StringLength(50, ErrorMessage = "Credit type cannot exceed 50 characters")]
    public string CreditType { get; set; } = string.Empty; // Full, Partial
    
    [Required(ErrorMessage = "Credit reason is required")]
    [StringLength(500, ErrorMessage = "Credit reason cannot exceed 500 characters")]
    public string CreditReason { get; set; } = string.Empty;
    
    [Range(0, double.MaxValue, ErrorMessage = "Subtotal must be non-negative")]
    public decimal? Subtotal { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Total tax amount must be non-negative")]
    public decimal? TotalTaxAmount { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Total discount amount must be non-negative")]
    public decimal? TotalDiscountAmount { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Total addon amount must be non-negative")]
    public decimal? TotalAddonAmount { get; set; }
    
    [Required(ErrorMessage = "Total amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than 0")]
    public decimal TotalAmount { get; set; }
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }
    
    public List<CreditNoteTaxDto>? Taxes { get; set; }
    
    public List<CreditNoteDiscountDto>? Discounts { get; set; }
    
    public List<CreditNoteAddOnDto>? AddOns { get; set; }
}

public class CreditNoteTaxDto
{
    [Required(ErrorMessage = "Tax ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Tax ID must be greater than 0")]
    public int TaxId { get; set; }
    
    [Required(ErrorMessage = "Tax amount is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Tax amount must be non-negative")]
    public decimal TaxAmount { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Taxable amount must be non-negative")]
    public decimal? TaxableAmount { get; set; }
}

public class CreditNoteDiscountDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Discount ID must be greater than 0")]
    public int? DiscountId { get; set; }
    
    [Required(ErrorMessage = "Discount amount is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Discount amount must be non-negative")]
    public decimal DiscountAmount { get; set; }
}

public class CreditNoteAddOnDto
{
    [Required(ErrorMessage = "Addon name is required")]
    [StringLength(200, ErrorMessage = "Addon name cannot exceed 200 characters")]
    public string AddonName { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Addon description cannot exceed 500 characters")]
    public string? AddonDescription { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; } = 1;
    
    [Required(ErrorMessage = "Unit price is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Unit price must be non-negative")]
    public decimal UnitPrice { get; set; }
    
    [Required(ErrorMessage = "Total price is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Total price must be non-negative")]
    public decimal TotalPrice { get; set; }
}
