using System;

namespace APIServiceManagement.Application.DTOs.Responses;

public class AuthResponse
{
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string RefreshToken { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    public UserResponse User { get; set; }
}
