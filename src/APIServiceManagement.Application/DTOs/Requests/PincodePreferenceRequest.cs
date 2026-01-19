namespace APIServiceManagement.Application.DTOs.Requests;

public class PincodePreferenceRequest
{
    public string Pincode { get; set; } = string.Empty;
    public bool IsPrimary { get; set; } = true;
}
