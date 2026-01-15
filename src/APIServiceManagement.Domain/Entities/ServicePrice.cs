using System;

namespace APIServiceManagement.Domain.Entities;

public class ServicePrice
{
    public int Id { get; set; }
    public decimal Charges { get; set; }
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? ServiceId { get; set; }

    // Navigation property
    public Service Service { get; set; }
}