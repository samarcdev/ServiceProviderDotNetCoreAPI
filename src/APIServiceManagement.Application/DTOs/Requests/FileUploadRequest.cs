using System;
using System.IO;

namespace APIServiceManagement.Application.DTOs.Requests;

public class FileUploadRequest
{
    public Func<Stream> OpenReadStream { get; set; } = default!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Length { get; set; }
}
