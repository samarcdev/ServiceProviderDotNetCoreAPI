using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("invoice_master")]
public class InvoiceMaster
{
    [Key] 
    [Column("invoice_id")]
    public int InvoiceId { get; set; }

    [Column("invoice_number")]
    public string? InvoiceNumber { get; set; }

    [Column("booking_id")]
    public Guid? BookingId { get; set; }

    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Column("service_provider_id")]
    public Guid? ServiceProviderId { get; set; }

    [Column("service_id")]
    public int ServiceId { get; set; }

    [Column("location_id")]
    public int? LocationId { get; set; }

    [Column("base_price")]
    public decimal BasePrice { get; set; }

    [Column("location_adjustment")]
    public decimal? LocationAdjustment { get; set; }

    [Column("total_add_on_amount")]
    public decimal? TotalAddOnAmount { get; set; }

    [Column("discount_amount")]
    public decimal? DiscountAmount { get; set; }

    [Column("tax_amount")]
    public decimal? TaxAmount { get; set; }

    [Column("final_amount")]
    public decimal FinalAmount { get; set; }

    [Column("invoice_date")]
    public DateTime? InvoiceDate { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("subtotal")]
    public decimal Subtotal { get; set; }

    [Column("total_tax_amount")]
    public decimal TotalTaxAmount { get; set; }

    [Column("total_discount_amount")]
    public decimal TotalDiscountAmount { get; set; }

    [Column("total_addon_amount")]
    public decimal TotalAddonAmount { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("payment_status")]
    public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Overdue, Cancelled

    [Column("pdf_path")]
    public string? PdfPath { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public BookingRequest Booking { get; set; }
    public User Customer { get; set; }
    public User? ServiceProvider { get; set; }
    public Service Service { get; set; }
    public ICollection<InvoiceTax> InvoiceTaxes { get; set; } = new List<InvoiceTax>();
    public ICollection<InvoiceDiscount> InvoiceDiscounts { get; set; } = new List<InvoiceDiscount>();
    public ICollection<InvoiceAddOn> InvoiceAddOns { get; set; } = new List<InvoiceAddOn>();
}
