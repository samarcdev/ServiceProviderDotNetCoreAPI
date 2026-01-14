using System;

namespace APIServiceManagement.Domain.Entities;

public class AdminStateAssignment
{
    public int Id { get; set; }
    public Guid AdminUserId { get; set; }
    public int StateId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public Guid? AssignedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User AdminUser { get; set; }
    public State State { get; set; }
    public User AssignedByUser { get; set; }
}