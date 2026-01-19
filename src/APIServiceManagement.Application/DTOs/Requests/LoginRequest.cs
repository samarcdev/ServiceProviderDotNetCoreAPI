using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Application.DTOs.Requests;

public class LoginRequest
{
    [Required]
    [Phone]
    [StringLength(20)]
    public string MobileNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; }
}
