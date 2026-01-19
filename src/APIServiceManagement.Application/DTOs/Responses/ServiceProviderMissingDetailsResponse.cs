using System;
using System.Collections.Generic;

namespace APIServiceManagement.Application.DTOs.Responses;

public class ServiceProviderMissingDetailsResponse
{
    public Guid UserId { get; set; }
    public string? BasePincode { get; set; }
    public string? PrimaryPincode { get; set; }
    public IReadOnlyList<string> NearbyPincodes { get; set; } = new List<string>();
    public IReadOnlyList<string> SelectedPincodes { get; set; } = new List<string>();
    public IReadOnlyList<string> RequiredDocuments { get; set; } = new List<string>();
    public IReadOnlyList<string> MissingDocuments { get; set; } = new List<string>();
    public bool HasExperience { get; set; }
    public string? Experience { get; set; }
    public bool HasPincodePreferences { get; set; }
    public bool IsComplete { get; set; }
}
