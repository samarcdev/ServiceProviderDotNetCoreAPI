using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("role_terms_conditions")]
public class RoleTermsCondition
{
    [Column("terms_conditions_id")]
    public int TermsConditionsId { get; set; }
    [Column("role_id")]
    public int RoleId { get; set; }
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public TermsAndCondition? TermsAndCondition { get; set; }
    public Role? Role { get; set; }
}