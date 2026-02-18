using System.Collections.Generic;

namespace APIServiceManagement.Application.DTOs.Requests;

public class CreateCreditNoteRequest
{
    public int InvoiceId { get; set; }
    public string CreditType { get; set; } = string.Empty; // Full, Partial
    public string CreditReason { get; set; } = string.Empty;
    public decimal? Subtotal { get; set; }
    public decimal? TotalTaxAmount { get; set; }
    public decimal? TotalDiscountAmount { get; set; }
    public decimal? TotalAddonAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public List<CreditNoteTaxDto>? Taxes { get; set; }
    public List<CreditNoteDiscountDto>? Discounts { get; set; }
    public List<CreditNoteAddOnDto>? AddOns { get; set; }
}

public class CreditNoteTaxDto
{
    public int TaxId { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal? TaxableAmount { get; set; }
}

public class CreditNoteDiscountDto
{
    public int? DiscountId { get; set; }
    public decimal DiscountAmount { get; set; }
}

public class CreditNoteAddOnDto
{
    public string AddonName { get; set; } = string.Empty;
    public string? AddonDescription { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
