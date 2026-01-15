using System;

namespace APIServiceManagement.Domain.Entities;

public class TermsAndCondition
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string DocumentName { get; set; }
    public string DocumentUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public RoleTermsCondition RoleTermsCondition { get; set; }
}