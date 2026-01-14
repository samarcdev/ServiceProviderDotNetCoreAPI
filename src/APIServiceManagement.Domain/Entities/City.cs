using System.Collections.Generic;

namespace APIServiceManagement.Domain.Entities;

public class City
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int? StateId { get; set; }
    public bool IsActive { get; set; } = true;
    public string Pincode { get; set; }

    // Navigation properties
    public State State { get; set; }
    public ICollection<UsersAddress> UsersAddresses { get; set; }
    public ICollection<ServiceAvailablePincode> ServiceAvailablePincodes { get; set; }
}