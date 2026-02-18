using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("credit_note_master")]
public class CreditNoteMaster
{
    [Key]
    [Column("credit_note_id")]
    public int CreditNoteId { get; set; }

    [Column("credit_note_number")]
    public string CreditNoteNumber { get; set; } = string.Empty;

    [Column("invoice_id")]
    public int InvoiceId { get; set; }

    [Column("booking_id")]
    public Guid? BookingId { get; set; }

    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Column("service_provider_id")]
    public Guid? ServiceProviderId { get; set; }

    [Column("service_id")]
    public int ServiceId { get; set; }

    [Column("credit_type")]
    public string CreditType { get; set; } = string.Empty; // Full, Partial

    [Column("credit_reason")]
    public string CreditReason { get; set; } = string.Empty;

    [Column("credit_note_date")]
    public DateTime CreditNoteDate { get; set; }

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

    [Column("status")]
    public string Status { get; set; } = "Issued"; // Issued, Applied, Cancelled

    [Column("pdf_path")]
    public string? PdfPath { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_by")]
    public Guid CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public InvoiceMaster Invoice { get; set; } = null!;
    public BookingRequest? Booking { get; set; }
    public User Customer { get; set; } = null!;
    public User? ServiceProvider { get; set; }
    public Service Service { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public ICollection<CreditNoteTax> CreditNoteTaxes { get; set; } = new List<CreditNoteTax>();
    public ICollection<CreditNoteDiscount> CreditNoteDiscounts { get; set; } = new List<CreditNoteDiscount>();
    public ICollection<CreditNoteAddOn> CreditNoteAddOns { get; set; } = new List<CreditNoteAddOn>();
    public ICollection<CreditNoteAuditHistory> AuditHistory { get; set; } = new List<CreditNoteAuditHistory>();
    public ICollection<CreditNoteApplication> Applications { get; set; } = new List<CreditNoteApplication>();
}
