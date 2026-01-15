using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Domain.Entities;

public class CityPincode
{
    [Key]
    public int Id { get; set; }
    public int CityId { get; set; }
    public string Pincode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public City? City { get; set; }
    public ICollection<ServiceAvailablePincode> ServiceAvailablePincodes { get; set; } = new List<ServiceAvailablePincode>();
}

