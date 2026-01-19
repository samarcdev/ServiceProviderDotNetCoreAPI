using System.Collections.Generic;

namespace APIServiceManagement.Application.DTOs.Requests;

public class ServiceProviderDetailsUpdateRequest
{
    public string? Experience { get; set; }
    public List<string>? Pincodes { get; set; }
    public string? PrimaryPincode { get; set; }
}
