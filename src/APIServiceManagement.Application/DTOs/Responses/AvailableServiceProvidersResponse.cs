using System.Collections.Generic;

namespace APIServiceManagement.Application.DTOs.Responses;

public class AvailableServiceProvidersResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<AvailableServiceProvider> ServiceProviders { get; set; } = new();
}

public class AvailableServiceProvider
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
}
