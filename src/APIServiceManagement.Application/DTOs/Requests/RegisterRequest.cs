using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Application.DTOs.Requests;

public class RegisterRequest
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

    [Required]
    [StringLength(50)]
    public string Role { get; set; }
}
