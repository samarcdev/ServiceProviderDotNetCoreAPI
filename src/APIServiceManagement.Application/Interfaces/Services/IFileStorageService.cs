using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Application.Interfaces.Services;

public interface IFileStorageService
{
    Task<FileStorageResult> SaveAsync(FileUploadRequest file, string subfolder, CancellationToken cancellationToken = default);
    Task<string> SaveFileAsync(byte[] fileBytes, string fileName, string subfolder, CancellationToken cancellationToken = default);
    Task<byte[]> GetFileAsync(string relativePath, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
}
