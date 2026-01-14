using APIServiceManagement.Domain.Enums;

namespace APIServiceManagement.Application.DTOs.Requests;

public class CreateUserRequest
{
    public string Name { get; set; }
    public string Email { get; set; }
    public UserStatus Status { get; set; }
}