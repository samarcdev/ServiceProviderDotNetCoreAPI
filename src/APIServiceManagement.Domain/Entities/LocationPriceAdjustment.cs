using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("location_price_adjustment")]
public class LocationPriceAdjustment
{
    [Key]
    [Column("location_id")]
    public int LocationId { get; set; }

    [Column("price_multiplier")]
    public decimal? PriceMultiplier { get; set; }

    [Column("fixed_adjustment")]
    public decimal? FixedAdjustment { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("city_pincode_id")]
    public int? CityPincodeId { get; set; }

    // Navigation property
    public CityPincode? CityPincode { get; set; }
}
