using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("users_extra_infos")]
public class UsersExtraInfo
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    [Column("user_id")]
    public Guid? UserId { get; set; }
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;
    [Column("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [Column("is_mobile_verified")]
    public bool IsMobileVerified { get; set; } = false;
    [Column("is_accepted_terms")]
    public bool IsAcceptedTerms { get; set; } = false;
    [Column("is_completed")]
    public bool IsCompleted { get; set; } = false;
    [Column("email")]
    public string Email { get; set; } = string.Empty;
    [Column("alternative_mobile")]
    public string AlternativeMobile { get; set; } = string.Empty;

    // Navigation properties
    public User? User { get; set; }
}
