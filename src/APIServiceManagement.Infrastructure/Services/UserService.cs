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
    private readonly IPasswordHasher _passwordHasher;

    public UserService(AppDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserResponse> GetUserByIdAsync(Guid id)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return null;
        }
        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Status = user.Status,
            Role = user.Role?.Name, 
            LastSignInAt = user.LastSignInAt
        };
    }

    public async Task<IEnumerable<UserResponse>> GetAllUsersAsync()
    {
        return await _context.Users
            .Include(u => u.Role)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Status = u.Status,
                Role = u.Role != null ? u.Role.Name : null,
                LastSignInAt = u.LastSignInAt
            })
            .ToListAsync();
    }

    public async Task CreateUserAsync(CreateUserRequest request)
    {
        var salt = string.IsNullOrWhiteSpace(request.Password) ? string.Empty : _passwordHasher.GenerateSalt();
        var hash = string.IsNullOrWhiteSpace(request.Password) ? string.Empty : _passwordHasher.HashPassword(request.Password, salt);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            PasswordSalt = salt,
            PasswordHash = hash,
            PasswordSlug = Guid.NewGuid().ToString("N"),
            RoleId = request.RoleId,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
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
            user.RoleId = request.RoleId;
            user.UpdatedAt = DateTime.UtcNow;
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