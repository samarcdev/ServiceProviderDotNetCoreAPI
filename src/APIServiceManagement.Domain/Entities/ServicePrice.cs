using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("service_prices")]
public class ServicePrice
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    [Column("charges")]
    public decimal Charges { get; set; }
    [Column("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    [Column("effective_from")]
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [Column("service_id")]
    public int? ServiceId { get; set; }

    // Navigation property
    public Service Service { get; set; }
}