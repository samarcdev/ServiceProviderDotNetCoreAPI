using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.API.DTOs.Requests;

public class ServiceProviderDocumentUploadRequest
{
    [Required]
    public string DocumentType { get; set; } = string.Empty;

    [Required]
    public IFormFile File { get; set; } = default!;
}
