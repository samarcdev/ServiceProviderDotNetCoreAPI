using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("service_provider_pincode_preferences")]
public class ServiceProviderPincodePreference
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Column("user_id")]
    public Guid UserId { get; set; }
    [Column("pincode")]
    public string Pincode { get; set; }
    [Column("is_primary")]
    public bool IsPrimary { get; set; } = false;
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; 

    // Navigation property
    public User User { get; set; }
}
