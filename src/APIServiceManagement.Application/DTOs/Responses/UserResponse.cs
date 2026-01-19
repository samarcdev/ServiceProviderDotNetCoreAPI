using APIServiceManagement.Domain.Enums;
using System;

namespace APIServiceManagement.Application.DTOs.Responses;

public class UserResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string MobileNumber { get; set; }
    public UserStatusEnum Status { get; set; }
    public string Role { get; set; }
    public DateTime? LastSignInAt { get; set; }
}