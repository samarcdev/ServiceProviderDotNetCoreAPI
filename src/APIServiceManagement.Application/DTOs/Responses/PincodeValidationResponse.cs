namespace APIServiceManagement.Application.DTOs.Responses;

public class PincodeValidationResponse
{
    public bool Valid { get; set; }
    public string? Message { get; set; }
    public int AvailableServicesCount { get; set; }
}
