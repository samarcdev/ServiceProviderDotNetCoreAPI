using System;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Domain.Entities;

public class ServiceAvailablePincode
{
    [Key]
    public int Id { get; set; }
    public decimal? PriceRating { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? ServiceId { get; set; }
    public int CityPincodeId { get; set; }
    public Guid? AdminId { get; set; }

    // Navigation properties
    public Service Service { get; set; }
    public CityPincode CityPincode { get; set; }
    public User Admin { get; set; }
}
