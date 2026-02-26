using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Application.DTOs.Requests;

public class ServiceProviderDetailsUpdateRequest
{
    [StringLength(500, ErrorMessage = "Experience cannot exceed 500 characters")]
    public string? Experience { get; set; }
    
    public List<string>? Pincodes { get; set; }
    
    [StringLength(10, MinimumLength = 6, ErrorMessage = "Primary pincode must be between 6 and 10 characters")]
    public string? PrimaryPincode { get; set; }
}
