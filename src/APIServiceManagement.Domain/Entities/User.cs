using APIServiceManagement.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    [Column("email")]
    public string Email { get; set; } = string.Empty;
    [Column("mobile_number")]
    public string MobileNumber { get; set; } = string.Empty;
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;
    [Column("password_salt")]
    public string PasswordSalt { get; set; } = string.Empty;
    [Column("password_slug")]
    public string PasswordSlug { get; set; } = string.Empty;
    [Column("role_id")]
    public int RoleId { get; set; }
    [Column("status_id")]
    public int StatusId { get; set; }
    [Column("verification_status_id")]
    public int VerificationStatusId { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [Column("last_sign_in_at")]
    public DateTime? LastSignInAt { get; set; } 
    [Column("refresh_token")]
    public string? RefreshToken { get; set; }
    [Column("refresh_token_expires_at")]
    public DateTime? RefreshTokenExpiresAt { get; set; }

    // Navigation properties
    public Role? Role { get; set; }
    public VerificationStatus? VerificationStatus { get; set; }
    public UserStatus? Status { get; set; }
}
