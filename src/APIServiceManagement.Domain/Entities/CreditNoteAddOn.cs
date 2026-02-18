using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("credit_note_add_ons")]
public class CreditNoteAddOn
{
    [Key]
    [Column("credit_note_addon_id")]
    public int CreditNoteAddonId { get; set; }

    [Column("credit_note_id")]
    public int CreditNoteId { get; set; }

    [Column("addon_name")]
    public string AddonName { get; set; } = string.Empty;

    [Column("addon_description")]
    public string? AddonDescription { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; } = 1;

    [Column("unit_price")]
    public decimal UnitPrice { get; set; }

    [Column("total_price")]
    public decimal TotalPrice { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public CreditNoteMaster CreditNote { get; set; } = null!;
}
