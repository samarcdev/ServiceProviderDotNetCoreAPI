using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace APIServiceManagement.Application.DTOs.Requests;

public class CreateAdminRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// List of state IDs to assign. Only the first state will be assigned (one admin can only have one state).
    /// If a state is already assigned to another admin, it will be skipped and reported in the response.
    /// </summary>
    public List<int> StateIds { get; set; } = new();
}

public class UpdateAdminStateAssignmentsRequest
{
    /// <summary>
    /// List of state IDs to assign. Only the first state will be assigned (one admin can only have one state).
    /// If a state is already assigned to another admin, it will be skipped and reported in the response.
    /// </summary>
    [Required]
    public List<int> StateIds { get; set; } = new();
}

public class CreateCategoryRequest
{
    [Required]
    [StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    public int? ParentId { get; set; }

    public IFormFile? Image { get; set; }

    public IFormFile? Icon { get; set; }
}

public class UpdateCategoryRequest
{
    [StringLength(100)]
    public string? CategoryName { get; set; }

    public int? ParentId { get; set; }

    public IFormFile? Image { get; set; }

    public IFormFile? Icon { get; set; }

    public bool? IsActive { get; set; }
}

public class CreateServiceRequest
{
    [Required]
    [StringLength(100)]
    public string ServiceName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    public IFormFile? Image { get; set; }

    public IFormFile? Icon { get; set; }
}

public class UpdateServiceRequest
{
    [StringLength(100)]
    public string? ServiceName { get; set; }

    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    public int? CategoryId { get; set; }

    public IFormFile? Image { get; set; }

    public IFormFile? Icon { get; set; }

    public bool? IsActive { get; set; }
}

public class CreateStateRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string Code { get; set; } = string.Empty;
}

public class UpdateStateRequest
{
    [StringLength(100)]
    public string? Name { get; set; }

    [StringLength(10)]
    public string? Code { get; set; }

    public bool? IsActive { get; set; }
}

public class CreateCityRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int StateId { get; set; }

    [StringLength(10)]
    public string? Pincode { get; set; }
}

public class UpdateCityRequest
{
    [StringLength(100)]
    public string? Name { get; set; }

    [Range(1, int.MaxValue)]
    public int? StateId { get; set; }

    [StringLength(10)]
    public string? Pincode { get; set; }

    public bool? IsActive { get; set; }
}

public class CreateCityPincodeRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int CityId { get; set; }

    [Required]
    [StringLength(10, MinimumLength = 6)]
    public string Pincode { get; set; } = string.Empty;
}

public class UpdateCityPincodeRequest
{
    [Range(1, int.MaxValue)]
    public int? CityId { get; set; }

    [StringLength(10, MinimumLength = 6)]
    public string? Pincode { get; set; }

    public bool? IsActive { get; set; }
}