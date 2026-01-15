using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using APIServiceManagement.Application.Interfaces.Services;
using APIServiceManagement.Domain.Constants;
using APIServiceManagement.Domain.Entities;
using APIServiceManagement.Domain.Enums;
using APIServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace APIServiceManagement.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedMobile = NormalizeMobile(request.MobileNumber);
        var existingUser = await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
        if (existingUser)
        {
            throw new InvalidOperationException("Email is already registered.");
        }
        var existingMobile = await _context.Users.AnyAsync(u => u.MobileNumber == normalizedMobile);
        if (existingMobile)
        {
            throw new InvalidOperationException("Mobile number is already registered.");
        }

        var roleName = string.IsNullOrWhiteSpace(request.Role) ? RoleNames.Customer : request.Role.Trim();
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName && r.IsActive);
        if (role == null)
        {
            throw new InvalidOperationException("Invalid role.");
        }

        var statusId = await GetStatusIdAsync("PENDING");
        if (!statusId.HasValue)
        {
            throw new InvalidOperationException("Verification status is not configured.");
        }

        var salt = _passwordHasher.GenerateSalt();
        var hash = _passwordHasher.HashPassword(request.Password, salt);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name?.Trim() ?? string.Empty,
            Email = normalizedEmail,
            MobileNumber = normalizedMobile,
            PasswordSalt = salt,
            PasswordHash = hash,
            PasswordSlug = Guid.NewGuid().ToString("N"),
            RoleId = role.Id,
            Status = UserStatus.Active, 
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var (refreshToken, refreshExpiresAt) = IssueRefreshToken(user);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return BuildAuthResponse(user, role.Name, refreshToken, refreshExpiresAt);
    }

    public async Task<AuthResponse> RegisterCustomerAsync(CustomerRegisterRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedMobile = NormalizeMobile(request.Phone);
        var existingUser = await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
        if (existingUser)
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var existingMobile = await _context.Users.AnyAsync(u => u.MobileNumber == normalizedMobile);
        if (existingMobile)
        {
            throw new InvalidOperationException("Mobile number is already registered.");
        }

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Passwords do not match.");
        }

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.Customer && r.IsActive);
        if (role == null)
        {
            throw new InvalidOperationException("Customer role is not configured.");
        }

        var statusId = await GetStatusIdAsync("PENDING");
        if (!statusId.HasValue)
        {
            throw new InvalidOperationException("Verification status is not configured.");
        }

        var salt = _passwordHasher.GenerateSalt();
        var hash = _passwordHasher.HashPassword(request.Password, salt);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = request.Name?.Trim() ?? string.Empty,
            Email = normalizedEmail,
            MobileNumber = normalizedMobile,
            PasswordSalt = salt,
            PasswordHash = hash,
            PasswordSlug = Guid.NewGuid().ToString("N"),
            RoleId = role.Id,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var extraInfo = new UsersExtraInfo
        {
            UserId = userId,
            FullName = request.Name?.Trim() ?? string.Empty,
            PhoneNumber = normalizedMobile,
            AlternativeMobile = string.IsNullOrWhiteSpace(request.AlternativeMobile) ? string.Empty : NormalizeMobile(request.AlternativeMobile),
            UserType = RoleNames.Customer,
            RoleId = role.Id,
            Email = normalizedEmail,
            StatusId = statusId.Value,
            IsAcceptedTerms = false,
            IsCompleted = true,
            IsActive = true
        };

        var (refreshToken, refreshExpiresAt) = IssueRefreshToken(user);
        _context.Users.Add(user);
        _context.UsersExtraInfos.Add(extraInfo);
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _context.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }

        return BuildAuthResponse(user, role.Name, refreshToken, refreshExpiresAt);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var normalizedMobile = NormalizeMobile(request.MobileNumber);
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.MobileNumber == normalizedMobile);

        if (user == null || user.Status != UserStatus.Active)
        {
            throw new InvalidOperationException("Invalid credentials.");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordSalt, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid credentials.");
        }

        user.LastSignInAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        var (refreshToken, refreshExpiresAt) = IssueRefreshToken(user);
        await _context.SaveChangesAsync();

        return BuildAuthResponse(user, user.Role?.Name ?? RoleNames.Customer, refreshToken, refreshExpiresAt);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new InvalidOperationException("Refresh token is required.");
        }

        var refreshToken = request.RefreshToken.Trim();
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

        if (user == null || user.Status != UserStatus.Active)
        {
            throw new InvalidOperationException("Invalid refresh token.");
        }

        if (!user.RefreshTokenExpiresAt.HasValue || user.RefreshTokenExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Refresh token expired.");
        }

        var (newRefreshToken, refreshExpiresAt) = IssueRefreshToken(user);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return BuildAuthResponse(user, user.Role?.Name ?? RoleNames.Customer, newRefreshToken, refreshExpiresAt);
    }

    private AuthResponse BuildAuthResponse(User user, string roleName, string refreshToken, DateTime refreshTokenExpiresAt)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");
        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured.");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured.");
        var expiresMinutes = int.TryParse(jwtSettings["ExpiresMinutes"], out var minutes) ? minutes : 120;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.MobilePhone, user.MobileNumber ?? string.Empty),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, roleName ?? RoleNames.Customer)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new AuthResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            User = new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                MobileNumber = user.MobileNumber ?? string.Empty,
                Status = user.Status,
                Role = roleName ?? RoleNames.Customer,
                LastSignInAt = user.LastSignInAt
            }
        };
    }

    private (string refreshToken, DateTime refreshTokenExpiresAt) IssueRefreshToken(User user)
    {
        var refreshTokenDays = GetRefreshTokenDays();
        var refreshToken = GenerateRefreshToken();
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays);

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = refreshTokenExpiresAt;

        return (refreshToken, refreshTokenExpiresAt);
    }

    private int GetRefreshTokenDays()
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        return int.TryParse(jwtSettings["RefreshTokenDays"], out var days) ? days : 7;
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    private async Task<int?> GetStatusIdAsync(string code)
    {
        return await _context.VerificationStatuses
            .Where(status => status.IsActive && status.Code == code)
            .Select(status => (int?)status.Id)
            .FirstOrDefaultAsync();
    }

    private static string NormalizeMobile(string mobileNumber)
    {
        return (mobileNumber ?? string.Empty).Trim();
    }
}
