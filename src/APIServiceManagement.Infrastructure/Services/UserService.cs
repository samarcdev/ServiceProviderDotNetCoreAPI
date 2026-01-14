using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using APIServiceManagement.Application.Interfaces.Services;
using APIServiceManagement.Domain.Entities;
using APIServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace APIServiceManagement.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserResponse> GetUserByIdAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return null;
        }
        return new UserResponse { Id = user.Id, Name = user.Name, Email = user.Email, Status = user.Status };
    }

    public async Task<IEnumerable<UserResponse>> GetAllUsersAsync()
    {
        return await _context.Users
            .Select(u => new UserResponse { Id = u.Id, Name = u.Name, Email = u.Email, Status = u.Status })
            .ToListAsync();
    }

    public async Task CreateUserAsync(CreateUserRequest request)
    {
        var user = new User { Id = Guid.NewGuid(), Name = request.Name, Email = request.Email, Status = request.Status, CreatedAt = DateTime.UtcNow };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateUserAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            user.Name = request.Name;
            user.Email = request.Email;
            user.Status = request.Status;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteUserAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}