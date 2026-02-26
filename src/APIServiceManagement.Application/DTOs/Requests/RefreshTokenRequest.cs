using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Application.DTOs.Requests;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}
