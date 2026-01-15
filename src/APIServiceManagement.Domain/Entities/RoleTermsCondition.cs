using System;

namespace APIServiceManagement.Domain.Entities;

public class RoleTermsCondition
{
    public int TermsConditionsId { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public TermsAndCondition TermsAndCondition { get; set; }
    public Role Role { get; set; }
}