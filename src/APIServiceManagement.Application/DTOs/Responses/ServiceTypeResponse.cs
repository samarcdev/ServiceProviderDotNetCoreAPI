namespace APIServiceManagement.Application.DTOs.Responses;

public class ServiceTypeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class ServiceTypesListResponse
{
    public bool Success { get; set; }
    public List<ServiceTypeResponse> ServiceTypes { get; set; } = new();
    public string? Message { get; set; }
}
