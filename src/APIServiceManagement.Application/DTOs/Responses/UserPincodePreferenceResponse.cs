using System;

namespace APIServiceManagement.Application.DTOs.Responses;

public class UserPincodePreferenceResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Pincode { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
