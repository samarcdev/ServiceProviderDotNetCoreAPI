using APIServiceManagement.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Application.Interfaces.Services;

public interface IRoleService
{
    Task<ServiceResult> GetRolesAsync(CancellationToken cancellationToken = default);
}
