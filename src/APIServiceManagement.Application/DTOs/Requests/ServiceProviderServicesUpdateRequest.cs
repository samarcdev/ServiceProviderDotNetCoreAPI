using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Application.DTOs.Requests;

public class ServiceProviderServicesUpdateRequest
{
    [Required(ErrorMessage = "Category ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Category ID must be greater than 0")]
    public int CategoryId { get; set; }
    
    [Required(ErrorMessage = "At least one service ID is required")]
    [MinLength(1, ErrorMessage = "At least one service ID is required")]
    public List<int> ServiceIds { get; set; } = new();
}
