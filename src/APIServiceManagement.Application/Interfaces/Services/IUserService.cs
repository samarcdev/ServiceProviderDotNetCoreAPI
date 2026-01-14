using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;

namespace APIServiceManagement.Application.Interfaces.Services;

public interface IUserService
{
    Task<UserResponse> GetUserByIdAsync(Guid id);
    Task<IEnumerable<UserResponse>> GetAllUsersAsync();
    Task CreateUserAsync(CreateUserRequest request);
    Task UpdateUserAsync(Guid id, UpdateUserRequest request);
    Task DeleteUserAsync(Guid id);
}