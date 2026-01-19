using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Application.DTOs.Requests;

public class UpdateVerificationStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty; // "approved", "rejected", "under_review"
    
    public string? Notes { get; set; }
    
    public string? RejectionReason { get; set; }
}
