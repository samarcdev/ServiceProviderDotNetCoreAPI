using System;

namespace APIServiceManagement.Domain.Entities;

public class ProviderService
{
    public Guid UserId { get; set; }
    public int ServiceId { get; set; }
    public string Availability { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; 

    // Navigation properties
    public User User { get; set; }
    public Service Service { get; set; }
}