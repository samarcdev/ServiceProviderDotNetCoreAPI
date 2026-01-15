using APIServiceManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Application.DTOs.Requests;

public class UpdateUserRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; }

    [Range(1, int.MaxValue)]
    public int RoleId { get; set; }

    [EnumDataType(typeof(UserStatus))]
    public UserStatus Status { get; set; }
}