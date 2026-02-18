using System;

namespace APIServiceManagement.Application.DTOs.Responses;

public class InvoiceResponse
{
    public int Id { get; set; }
    public string? InvoiceNumber { get; set; }
    public Guid? BookingId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? ServiceProviderId { get; set; }
    public int ServiceId { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal BasePrice { get; set; }
    public decimal? LocationAdjustmentAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? CgstAmount { get; set; }
    public decimal? SgstAmount { get; set; }
    public decimal? IgstAmount { get; set; }
    public decimal? ServiceChargeAmount { get; set; }
    public decimal? PlatformChargeAmount { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? PdfPath { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public InvoiceServiceDto? Service { get; set; }
    public InvoiceCustomerDto? Customer { get; set; }
    public InvoiceServiceProviderDto? ServiceProvider { get; set; }
    public InvoiceCompanyDto? Company { get; set; }
}

public class InvoiceCompanyDto
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

public class InvoiceServiceDto
{
    public int Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class InvoiceCustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
}

public class InvoiceServiceProviderDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
}

public class InvoicePdfResponse
{
    public byte[] PdfBytes { get; set; } = Array.Empty<byte>();
    public string InvoiceNumber { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
}

public class InvoiceListResponse
{
    public List<InvoiceResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}