using System;

namespace APIServiceManagement.Domain.Entities;

public class UsersExtraInfo
{
    public int Id { get; set; }
    public Guid? UserId { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string UserType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsMobileVerified { get; set; } = false;
    public bool IsAcceptedTerms { get; set; } = false;
    public int? RoleId { get; set; }
    public bool IsCompleted { get; set; } = false;
    public string Email { get; set; }
    public bool IsVerified { get; set; } = false;
    public string AlternativeMobile { get; set; }
    public string VerificationStatus { get; set; } = "pending";
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public User User { get; set; }
    public Role Role { get; set; }
}