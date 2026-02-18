using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("credit_note_discounts")]
public class CreditNoteDiscount
{
    [Key]
    [Column("credit_note_discount_id")]
    public int CreditNoteDiscountId { get; set; }

    [Column("credit_note_id")]
    public int CreditNoteId { get; set; }

    [Column("discount_id")]
    public int? DiscountId { get; set; }

    [Column("discount_name")]
    public string? DiscountName { get; set; }

    [Column("discount_type")]
    public string? DiscountType { get; set; }

    [Column("discount_value")]
    public decimal? DiscountValue { get; set; }

    [Column("discount_amount")]
    public decimal DiscountAmount { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public CreditNoteMaster CreditNote { get; set; } = null!;
    public DiscountMaster? Discount { get; set; }
}
