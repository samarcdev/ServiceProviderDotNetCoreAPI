using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("invoice_discounts")]
public class InvoiceDiscount
{
    [Key]
    [Column("invoice_discount_id")]
    public int InvoiceDiscountId { get; set; }

    [Column("invoice_id")]
    public int InvoiceId { get; set; }

    [Column("discount_id")]
    public int DiscountId { get; set; }

    [Column("discount_name")]
    public string? DiscountName { get; set; }

    [Column("discount_type")]
    public string? DiscountType { get; set; } // Percentage, Fixed

    [Column("discount_value")]
    public decimal? DiscountValue { get; set; }

    [Column("discount_amount")]
    public decimal DiscountAmount { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    // Navigation properties
    public InvoiceMaster Invoice { get; set; }
    public DiscountMaster? Discount { get; set; }
}
