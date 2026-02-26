using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Application.DTOs.Requests;

public class PincodePreferenceRequest
{
    [Required(ErrorMessage = "Pincode is required")]
    [StringLength(10, MinimumLength = 6, ErrorMessage = "Pincode must be between 6 and 10 characters")]
    public string Pincode { get; set; } = string.Empty;
    
    public bool IsPrimary { get; set; } = true;
}
