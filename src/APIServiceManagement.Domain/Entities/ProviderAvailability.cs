using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("provider_availability")]
public class ProviderAvailability
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("service_provider_id")]
    public Guid ServiceProviderId { get; set; }

    /// <summary>
    /// Business date for which this availability session applies.
    /// </summary>
    [Column("business_date")]
    public DateTime BusinessDate { get; set; }

    [Column("check_in_time_utc")]
    public DateTime CheckInTimeUtc { get; set; }

    [Column("check_out_time_utc")]
    public DateTime? CheckOutTimeUtc { get; set; }

    /// <summary>
    /// Indicates whether the provider is currently checked in for this session.
    /// When false, this record is kept for history but should not be treated as active.
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("check_in_latitude")]
    public double? CheckInLatitude { get; set; }

    [Column("check_in_longitude")]
    public double? CheckInLongitude { get; set; }

    [Column("check_out_latitude")]
    public double? CheckOutLatitude { get; set; }

    [Column("check_out_longitude")]
    public double? CheckOutLongitude { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User ServiceProvider { get; set; }
}

