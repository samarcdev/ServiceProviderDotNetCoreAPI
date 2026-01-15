using System;
using System.Collections.Generic;

namespace APIServiceManagement.Domain.Entities;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public RoleTermsCondition RoleTermsCondition { get; set; }
    public ICollection<UsersExtraInfo> UsersExtraInfos { get; set; }
    public ICollection<User> Users { get; set; }
}