using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("city_pincodes")]
public class CityPincode
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    [Column("city_id")]
    public int CityId { get; set; }
    [Column("pincode")]
    public string Pincode { get; set; } = string.Empty;
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public City? City { get; set; }
}

