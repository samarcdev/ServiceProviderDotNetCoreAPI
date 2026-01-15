using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using APIServiceManagement.Application.Interfaces.Services;
using APIServiceManagement.Domain.Constants;
using APIServiceManagement.Domain.Entities;
using APIServiceManagement.Domain.Enums;
using APIServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Infrastructure.Services;

public class ServiceProviderRegistrationService : IServiceProviderRegistrationService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public ServiceProviderRegistrationService(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<RegistrationResponse> RegisterAsync(ServiceProviderRegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(request.BasicInfo.Password, request.BasicInfo.ConfirmPassword, StringComparison.Ordinal))
        {
            return new RegistrationResponse
            {
                Success = false,
                Message = "Validation failed.",
                Errors = new Dictionary<string, string> { ["basic_info.confirm_password"] = "Passwords do not match." }
            };
        }

        var role = await _context.Roles.FirstOrDefaultAsync(
            r => r.Name == RoleNames.ServiceProvider && r.IsActive,
            cancellationToken);
        if (role == null)
        {
            return new RegistrationResponse
            {
                Success = false,
                Message = "Service provider role is not configured.",
                Errors = new Dictionary<string, string> { ["role"] = "Invalid role." }
            };
        }

        var statusId = await GetStatusIdAsync("PENDING", cancellationToken);
        if (!statusId.HasValue)
        {
            return new RegistrationResponse
            {
                Success = false,
                Message = "Verification status is not configured.",
                Errors = new Dictionary<string, string> { ["status"] = "Verification status is not configured." }
            };
        }

        var stateExists = await _context.States.AnyAsync(
            state => state.Id == request.Address.StateId && state.IsActive,
            cancellationToken);
        if (!stateExists)
        {
            return new RegistrationResponse
            {
                Success = false,
                Message = "Validation failed.",
                Errors = new Dictionary<string, string> { ["address.state_id"] = "Selected state is invalid." }
            };
        }

        var cityExists = await _context.Cities.AnyAsync(
            city => city.Id == request.Address.CityId && city.IsActive,
            cancellationToken);
        if (!cityExists)
        {
            return new RegistrationResponse
            {
                Success = false,
                Message = "Validation failed.",
                Errors = new Dictionary<string, string> { ["address.city_id"] = "Selected city is invalid." }
            };
        }

        var normalizedEmail = request.BasicInfo.Email.Trim().ToLowerInvariant();
        var normalizedMobile = NormalizeMobile(request.BasicInfo.PhoneNumber);
        var normalizedAltMobile = string.IsNullOrWhiteSpace(request.BasicInfo.AlternativeMobile)
            ? string.Empty
            : NormalizeMobile(request.BasicInfo.AlternativeMobile);

        var userByPhone = await _context.Users
            .FirstOrDefaultAsync(u => u.MobileNumber == normalizedMobile, cancellationToken);
        var userByEmail = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (userByPhone != null && userByEmail != null && userByPhone.Id != userByEmail.Id)
        {
            return new RegistrationResponse
            {
                Success = false,
                Message = "Email is already registered.",
                Errors = new Dictionary<string, string> { ["basic_info.email"] = "Email is already registered." }
            };
        }

        var user = userByPhone ?? userByEmail;
        UsersExtraInfo? extraInfo = null;

        if (user != null)
        {
            if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                return new RegistrationResponse
                {
                    Success = false,
                    Message = "Mobile number is already registered.",
                    Errors = new Dictionary<string, string> { ["basic_info.phone_number"] = "Mobile number is already registered." }
                };
            }

            if (!string.IsNullOrWhiteSpace(user.MobileNumber) && user.MobileNumber != normalizedMobile)
            {
                return new RegistrationResponse
                {
                    Success = false,
                    Message = "Mobile number is already registered.",
                    Errors = new Dictionary<string, string> { ["basic_info.phone_number"] = "Mobile number is already registered." }
                };
            }

            extraInfo = await _context.UsersExtraInfos.FirstOrDefaultAsync(x => x.UserId == user.Id, cancellationToken);
            if (extraInfo?.IsCompleted == true)
            {
                return new RegistrationResponse
                {
                    Success = false,
                    Message = "Email is already registered.",
                    Errors = new Dictionary<string, string> { ["basic_info.email"] = "Email is already registered." }
                };
            }
        }

        var salt = _passwordHasher.GenerateSalt();
        var hash = _passwordHasher.HashPassword(request.BasicInfo.Password, salt);

        try
        {
            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Name = request.BasicInfo.FullName?.Trim() ?? string.Empty,
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
                _context.Users.Add(user);
            }
            else
            {
                user.Name = request.BasicInfo.FullName?.Trim() ?? string.Empty;
                user.Email = normalizedEmail;
                user.MobileNumber = normalizedMobile;
                user.PasswordSalt = salt;
                user.PasswordHash = hash;
                user.RoleId = role.Id;
                user.UpdatedAt = DateTime.UtcNow;
            }

            if (extraInfo == null)
            {
                extraInfo = new UsersExtraInfo
                {
                    UserId = user.Id,
                    FullName = request.BasicInfo.FullName?.Trim() ?? string.Empty,
                    PhoneNumber = normalizedMobile,
                    AlternativeMobile = normalizedAltMobile,
                    UserType = RoleNames.ServiceProvider,
                    RoleId = role.Id,
                    Email = normalizedEmail,
                    StatusId = statusId.Value,
                    IsAcceptedTerms = false,
                    IsCompleted = true,
                    IsActive = true
                };
                _context.UsersExtraInfos.Add(extraInfo);
            }
            else
            {
                extraInfo.FullName = request.BasicInfo.FullName?.Trim() ?? string.Empty;
                extraInfo.PhoneNumber = normalizedMobile;
                extraInfo.AlternativeMobile = normalizedAltMobile;
                extraInfo.UserType = RoleNames.ServiceProvider;
                extraInfo.RoleId = role.Id;
                extraInfo.Email = normalizedEmail;
                extraInfo.StatusId = statusId.Value;
                extraInfo.IsCompleted = true;
                extraInfo.UpdatedAt = DateTime.UtcNow;
            }

            var address = await _context.UsersAddresses
                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.IsActive, cancellationToken);

            if (address == null)
            {
                address = new UsersAddress
                {
                    UserId = user.Id,
                    IsActive = true
                };
                _context.UsersAddresses.Add(address);
            }

            address.AddressLine1 = request.Address.AddressLine1?.Trim() ?? string.Empty;
            address.AddressLine2 = request.Address.AddressLine2?.Trim() ?? string.Empty;
            address.Street = request.Address.Street?.Trim() ?? string.Empty;
            address.ZipCode = request.Address.ZipCode?.Trim() ?? string.Empty;
            address.CityId = request.Address.CityId;
            address.StateId = request.Address.StateId;
            address.UpdatedAt = DateTime.UtcNow;

            var verification = await _context.ServiceProviderVerifications
                .FirstOrDefaultAsync(v => v.ProviderUserId == user.Id && v.IsActive, cancellationToken);

            if (verification == null)
            {
                _context.ServiceProviderVerifications.Add(new ServiceProviderVerification
                {
                    ProviderUserId = user.Id,
                    VerificationStatusId = statusId.Value,
                    VerificationNotes = string.Empty,
                    RejectionReason = string.Empty,
                    IsActive = true
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            throw;
        }

        return new RegistrationResponse
        {
            Success = true,
            UserId = user.Id,
            Message = "Service provider registration completed successfully."
        };
    }

    private async Task<int?> GetStatusIdAsync(string code, CancellationToken cancellationToken)
    {
        return await _context.VerificationStatuses
            .Where(status => status.IsActive && status.Code == code)
            .Select(status => (int?)status.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string NormalizeMobile(string mobileNumber)
    {
        return (mobileNumber ?? string.Empty).Trim();
    }
}
