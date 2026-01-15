using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Domain.Entities;

public class VerificationStatus
{
    [Key]
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<UsersExtraInfo> UsersExtraInfos { get; set; } = new List<UsersExtraInfo>();
}
