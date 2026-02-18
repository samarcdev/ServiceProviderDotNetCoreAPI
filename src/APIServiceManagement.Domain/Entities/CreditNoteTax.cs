using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("credit_note_taxes")]
public class CreditNoteTax
{
    [Key]
    [Column("credit_note_tax_id")]
    public int CreditNoteTaxId { get; set; }

    [Column("credit_note_id")]
    public int CreditNoteId { get; set; }

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
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public CreditNoteMaster CreditNote { get; set; } = null!;
    public TaxMaster Tax { get; set; } = null!;
}
