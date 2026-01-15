using APIServiceManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Application.DTOs.Requests;

public class CreateUserRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; }

    [Required]
    [Phone]
    [StringLength(20)]
    public string MobileNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; }

    [Range(1, int.MaxValue)]
    public int RoleId { get; set; }

    [EnumDataType(typeof(UserStatus))]
    public UserStatus Status { get; set; }
}