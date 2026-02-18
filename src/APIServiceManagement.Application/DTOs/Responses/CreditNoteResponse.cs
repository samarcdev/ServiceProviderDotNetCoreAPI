using System;
using System.Collections.Generic;

namespace APIServiceManagement.Application.DTOs.Responses;

public class CreditNoteResponse
{
    public int Id { get; set; }
    public string CreditNoteNumber { get; set; } = string.Empty;
    public int InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public Guid? BookingId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? ServiceProviderId { get; set; }
    public int ServiceId { get; set; }
    public string CreditType { get; set; } = string.Empty;
    public string CreditReason { get; set; } = string.Empty;
    public DateTime CreditNoteDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal TotalDiscountAmount { get; set; }
    public decimal TotalAddonAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PdfPath { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public CreditNoteServiceDto? Service { get; set; }
    public CreditNoteCustomerDto? Customer { get; set; }
    public CreditNoteServiceProviderDto? ServiceProvider { get; set; }
    public CreditNoteCompanyDto? Company { get; set; }
    public List<CreditNoteTaxResponseDto>? Taxes { get; set; }
    public List<CreditNoteDiscountResponseDto>? Discounts { get; set; }
    public List<CreditNoteAddOnResponseDto>? AddOns { get; set; }
    public List<CreditNoteApplicationResponseDto>? Applications { get; set; }
}

public class CreditNoteServiceDto
{
    public int Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreditNoteCustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
}

public class CreditNoteServiceProviderDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
}

public class CreditNoteCompanyDto
{
    public string? CompanyName { get; set; }
    public string? CompanyAddressLine1 { get; set; }
    public string? CompanyAddressLine2 { get; set; }
    public string? CompanyCity { get; set; }
    public string? CompanyPincode { get; set; }
    public string? CompanyState { get; set; }
    public string? CompanyPhone { get; set; }
    public string? CompanyEmail { get; set; }
    public string? CompanyGstin { get; set; }
    public string? CompanyPan { get; set; }
    public string? CompanyWebsite { get; set; }
    public string? InvoiceFooterText { get; set; }
}

public class CreditNoteTaxResponseDto
{
    public int Id { get; set; }
    public int TaxId { get; set; }
    public string? TaxName { get; set; }
    public decimal? TaxPercentage { get; set; }
    public decimal? TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
}

public class CreditNoteDiscountResponseDto
{
    public int Id { get; set; }
    public int? DiscountId { get; set; }
    public string? DiscountName { get; set; }
    public string? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal DiscountAmount { get; set; }
}

public class CreditNoteAddOnResponseDto
{
    public int Id { get; set; }
    public string AddonName { get; set; } = string.Empty;
    public string? AddonDescription { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CreditNoteApplicationResponseDto
{
    public int Id { get; set; }
    public decimal AppliedAmount { get; set; }
    public DateTime ApplicationDate { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankName { get; set; }
    public string? TransactionReference { get; set; }
    public Guid AppliedBy { get; set; }
    public string? AppliedByName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreditNotePdfResponse
{
    public byte[] PdfBytes { get; set; } = Array.Empty<byte>();
    public string CreditNoteNumber { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
}

public class CreditNoteSummaryReportResponse
{
    public decimal TotalCreditsIssued { get; set; }
    public decimal TotalCreditsApplied { get; set; }
    public decimal TotalCreditsCancelled { get; set; }
    public int TotalCount { get; set; }
    public int IssuedCount { get; set; }
    public int AppliedCount { get; set; }
    public int CancelledCount { get; set; }
    public DateTime? ReportStartDate { get; set; }
    public DateTime? ReportEndDate { get; set; }
}

public class CreditNoteListResponse
{
    public List<CreditNoteResponse> CreditNotes { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
