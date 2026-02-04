using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("tax_master")]
public class TaxMaster
{
    [Key]
    [Column("tax_id")]
    public int TaxId { get; set; }

    [Column("tax_name")]
    public string TaxName { get; set; } = string.Empty;

    [Column("tax_percentage")]
    public decimal TaxPercentage { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }
}
