using APIServiceManagement.Domain.Enums;
using System;

namespace APIServiceManagement.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string PasswordSlug { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public UserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSignInAt { get; set; } 

    // Navigation properties
    public Role? Role { get; set; }
}