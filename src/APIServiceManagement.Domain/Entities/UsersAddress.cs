using System;

namespace APIServiceManagement.Domain.Entities;

public class UsersAddress
{
    public int Id { get; set; }
    public Guid? UserId { get; set; }
    public string Street { get; set; }
    public string ZipCode { get; set; }
    public int? CityId { get; set; }
    public bool IsActive { get; set; } = false;
    public string AddressLine1 { get; set; }
    public string AddressLine2 { get; set; }
    public int? StateId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; }
    public City City { get; set; }
    public State State { get; set; }
}