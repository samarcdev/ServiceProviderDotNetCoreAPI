using System;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Domain.Entities;

public class UsersExtraInfo
{
    [Key]
    public int Id { get; set; }
    public Guid? UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsMobileVerified { get; set; } = false;
    public bool IsAcceptedTerms { get; set; } = false;
    public int? RoleId { get; set; }
    public bool IsCompleted { get; set; } = false;
    public string Email { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string AlternativeMobile { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public User? User { get; set; }
    public Role? Role { get; set; }
    public VerificationStatus? Status { get; set; }
}
