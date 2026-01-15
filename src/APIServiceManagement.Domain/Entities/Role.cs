using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Domain.Entities;

public class Role
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<RoleTermsCondition> RoleTermsConditions { get; set; }
    public ICollection<UsersExtraInfo> UsersExtraInfos { get; set; }
    public ICollection<User> Users { get; set; }
}