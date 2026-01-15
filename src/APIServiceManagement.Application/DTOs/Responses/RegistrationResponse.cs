using System;
using System.Collections.Generic;

namespace APIServiceManagement.Application.DTOs.Responses;

public class RegistrationResponse
{
    public bool Success { get; set; }
    public Guid? UserId { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, string>? Errors { get; set; }
}
