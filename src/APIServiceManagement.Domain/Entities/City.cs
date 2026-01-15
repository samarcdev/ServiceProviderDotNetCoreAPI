using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Domain.Entities;

public class City
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public int? StateId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public State State { get; set; }
    public ICollection<UsersAddress> UsersAddresses { get; set; }
    public ICollection<ServiceAvailablePincode> ServiceAvailablePincodes { get; set; }
    public ICollection<CityPincode> CityPincodes { get; set; }
}