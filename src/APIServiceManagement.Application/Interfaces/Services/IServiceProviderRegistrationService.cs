using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Application.Interfaces.Services;

public interface IServiceProviderRegistrationService
{
    Task<RegistrationResponse> RegisterAsync(ServiceProviderRegisterRequest request, CancellationToken cancellationToken = default);
}
