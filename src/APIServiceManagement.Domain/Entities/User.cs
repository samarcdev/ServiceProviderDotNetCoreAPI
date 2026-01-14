using APIServiceManagement.Domain.Enums;

namespace APIServiceManagement.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}