using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("provider_services")]
public class ProviderService
{
    [Column("user_id")]
    public Guid UserId { get; set; }
    [Column("service_id")]
    public int ServiceId { get; set; }
    [Column("availability")]
    public string Availability { get; set; }
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; 

    // Navigation properties
    public User User { get; set; }
    public Service Service { get; set; }
}