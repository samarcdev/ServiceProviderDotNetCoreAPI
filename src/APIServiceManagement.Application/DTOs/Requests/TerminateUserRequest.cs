using System;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Application.DTOs.Requests;

public class TerminateUserRequest
{
    [Required(ErrorMessage = "User ID is required")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Reason is required. Please provide a solid reason for termination.")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Reason must be between 10 and 1000 characters")]
    public string Reason { get; set; } = string.Empty;
}
