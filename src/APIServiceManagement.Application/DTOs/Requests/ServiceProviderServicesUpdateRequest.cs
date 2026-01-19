using System.Collections.Generic;

namespace APIServiceManagement.Application.DTOs.Requests;

public class ServiceProviderServicesUpdateRequest
{
    public int CategoryId { get; set; }
    public List<int> ServiceIds { get; set; } = new();
}
