using APIServiceManagement.Domain.Enums;

namespace APIServiceManagement.Application.DTOs.Responses;

public class UserResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public UserStatus Status { get; set; }
}