using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("invoice_taxes")]
public class InvoiceTax
{
    [Key]
    [Column("invoice_tax_id")]
    public int InvoiceTaxId { get; set; }

    [Column("invoice_id")]
    public int InvoiceId { get; set; }

    [Column("tax_id")]
    public int TaxId { get; set; }

    [Column("tax_name")]
    public string? TaxName { get; set; }

    [Column("tax_percentage")]
    public decimal? TaxPercentage { get; set; }

    [Column("taxable_amount")]
    public decimal? TaxableAmount { get; set; }

    [Column("tax_amount")]
    public decimal TaxAmount { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    // Navigation properties
    public InvoiceMaster Invoice { get; set; }
    public TaxMaster Tax { get; set; }
}
