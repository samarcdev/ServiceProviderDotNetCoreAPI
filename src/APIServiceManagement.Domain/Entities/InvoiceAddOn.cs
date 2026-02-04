using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("invoice_add_ons")]
public class InvoiceAddOn
{
    [Key]
    [Column("invoice_add_on_id")]
    public int InvoiceAddonId { get; set; }

    [Column("invoice_id")]
    public int InvoiceId { get; set; }

    [Column("add_on_id")]
    public int AddOnId { get; set; }

    [Column("addon_name")]
    public string? AddonName { get; set; }

    [Column("addon_description")]
    public string? AddonDescription { get; set; }

    [Column("quantity")]
    public int? Quantity { get; set; } = 1;

    [Column("price")]
    public decimal Price { get; set; }

    [Column("unit_price")]
    public decimal? UnitPrice { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("total_price")]
    public decimal? TotalPrice { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    // Navigation properties
    public InvoiceMaster Invoice { get; set; }
}
