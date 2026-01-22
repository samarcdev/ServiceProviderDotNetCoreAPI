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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Infrastructure.Services;

public class ServiceProviderProfileService : IServiceProviderProfileService
{
    private static readonly string[] RequiredDocumentTypes =
    {
        "aadhar_front",
        "aadhar_back"
    };

    private static readonly string[] OptionalDocumentTypes =
    {
        "tenth_certificate",
        "twelfth_certificate",
        "experience_certificate"
    };

    private const string ExperienceStepName = "service_provider_details";

    private readonly AppDbContext _context;
    private readonly IFileStorageService _fileStorageService;

    public ServiceProviderProfileService(AppDbContext context, IFileStorageService fileStorageService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
    }

    public async Task<ServiceResult> GetProfileAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        if (!userId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.VerificationStatus)
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        if (user == null)
        {
            return ServiceResult.NotFound(new MessageResponse { Message = "Service provider not found." });
        }

        var extraInfo = await _context.UsersExtraInfos
            .AsNoTracking()
            .FirstOrDefaultAsync(info => info.UserId == userId.Value, cancellationToken);

        var address = await _context.UsersAddresses
            .AsNoTracking()
            .Include(a => a.City)
            .ThenInclude(city => city.State)
            .FirstOrDefaultAsync(a => a.UserId == userId.Value && a.IsActive, cancellationToken);

        var verification = await _context.ServiceProviderVerifications
            .AsNoTracking()
            .Include(v => v.AssignedAdmin)
            .Include(v => v.VerifiedByUser)
            .FirstOrDefaultAsync(v => v.ProviderUserId == userId.Value && v.IsActive, cancellationToken);

        var documents = await _context.Documents
            .AsNoTracking()
            .Where(doc => doc.UserId == userId.Value)
            .OrderByDescending(doc => doc.UploadedAt)
            .ToListAsync(cancellationToken);

        var pincodePreferences = await _context.ServiceProviderPincodePreferences
            .AsNoTracking()
            .Where(pref => pref.UserId == userId.Value)
            .OrderByDescending(pref => pref.IsPrimary)
            .ThenByDescending(pref => pref.CreatedAt)
            .ToListAsync(cancellationToken);

        var providerServices = await _context.ProviderServices
            .AsNoTracking()
            .Include(ps => ps.Service)
            .Where(ps => ps.UserId == userId.Value)
            .ToListAsync(cancellationToken);

        var experienceStep = await _context.UserRegistrationSteps
            .AsNoTracking()
            .Where(step => step.UserId == userId.Value && step.StepName == ExperienceStepName)
            .OrderByDescending(step => step.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var experience = ExtractExperience(experienceStep?.StepData);

        var response = new ServiceProviderProfileResponse
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email,
            IsVerified=(user.VerificationStatusId==(int)VerificationStatusEnum.Approved),
            MobileNumber = user.MobileNumber,
            ExtraInfo = extraInfo == null
                ? null
                : new ServiceProviderExtraInfoResponse
                {
                    FullName = extraInfo.FullName,
                    PhoneNumber = extraInfo.PhoneNumber,
                    AlternativeMobile = extraInfo.AlternativeMobile,
                    Email = extraInfo.Email,
                    StatusId = user.StatusId, 
                    StatusCode = user.VerificationStatus?.Code,
                    StatusName = user.VerificationStatus?.Name,
                    IsMobileVerified = extraInfo.IsMobileVerified,
                    IsAcceptedTerms = extraInfo.IsAcceptedTerms,
                    IsCompleted = extraInfo.IsCompleted
                },
            Address = address == null
                ? null
                : new ServiceProviderAddressResponse
                {
                    AddressLine1 = address.AddressLine1,
                    AddressLine2 = address.AddressLine2,
                    Street = address.Street,
                    ZipCode = address.ZipCode,
                    CityId = address.CityId,
                    CityName = address.City?.Name,
                    StateId = address.StateId,
                    StateName = address.City?.State?.Name ?? address.State?.Name
                },
            Verification = verification == null
                ? null
                : new ServiceProviderProfileVerificationResponse
                {
                    VerificationStatusId = user.VerificationStatusId,
                    VerificationStatusCode = user.VerificationStatus?.Code,
                    VerificationStatusName = user.VerificationStatus?.Name,
                    VerificationNotes = verification.VerificationNotes,
                    RejectionReason = verification.RejectionReason,
                    VerifiedAt = verification.VerifiedAt,
                    CreatedAt = verification.CreatedAt,
                    VerifiedBy = verification.VerifiedBy,
                    VerifiedByName = verification.VerifiedByUser?.Name,
                    AssignedAdminId = verification.AssignedAdminId,
                    AssignedAdminName = verification.AssignedAdmin?.Name
                },
            Documents = documents.Select(doc => new ServiceProviderDocumentResponse
            {
                DocumentId = doc.Id,
                DocumentType = doc.DocumentType,
                FileUrl = doc.FileUrl,
                FileName = doc.FileName,
                FileSize = doc.FileSize ?? 0,
                UploadedAt = doc.UploadedAt,
                IsActive = doc.IsActive
            }).ToList(),
            PincodePreferences = pincodePreferences.Select(pref => new UserPincodePreferenceResponse
            {
                Id = pref.Id,
                UserId = pref.UserId,
                Pincode = pref.Pincode,
                IsPrimary = pref.IsPrimary,
                CreatedAt = pref.CreatedAt,
                UpdatedAt = pref.UpdatedAt
            }).ToList(),
            Services = providerServices.Select(ps => new ServiceProviderServiceResponse
            {
                ServiceId = ps.ServiceId,
                ServiceName = ps.Service?.ServiceName ?? string.Empty,
                Description = ps.Service?.Description,
                CategoryId = ps.Service?.CategoryId ?? 0,
                Availability = ps.Availability
            }).ToList(),
            Experience = experience,
            HasExperience = !string.IsNullOrWhiteSpace(experience)
        };

        return ServiceResult.Ok(response);
    }

    public async Task<ServiceResult> GetMissingDetailsAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        if (!userId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }
        var extraInfo = await _context.UsersExtraInfos
            .AsNoTracking()
            .FirstOrDefaultAsync(info => info.UserId == userId.Value, cancellationToken);

        var address = await _context.UsersAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId.Value && a.IsActive, cancellationToken);

        var basePincode = address?.ZipCode?.Trim();

        var preferences = await _context.ServiceProviderPincodePreferences
            .AsNoTracking()
            .Where(pref => pref.UserId == userId.Value)
            .OrderByDescending(pref => pref.IsPrimary)
            .ThenByDescending(pref => pref.CreatedAt)
            .ToListAsync(cancellationToken);

        var primaryPincode = preferences.FirstOrDefault(pref => pref.IsPrimary)?.Pincode;
        var selectedPincodes = preferences.Select(pref => pref.Pincode).ToList();

        var nearbyPincodes = new List<string>();
        if (!string.IsNullOrWhiteSpace(basePincode))
        {
            var pincodeEntry = await _context.CityPincodes
                .AsNoTracking()
                .FirstOrDefaultAsync(cp => cp.IsActive && cp.Pincode == basePincode, cancellationToken);

            if (pincodeEntry != null)
            {
                nearbyPincodes = await _context.CityPincodes
                    .AsNoTracking()
                    .Where(cp => cp.IsActive && cp.CityId == pincodeEntry.CityId)
                    .OrderBy(cp => cp.Pincode)
                    .Select(cp => cp.Pincode)
                    .ToListAsync(cancellationToken);
            }
        }

        var existingDocumentTypes = await _context.Documents
            .AsNoTracking()
            .Where(doc => doc.UserId == userId.Value && doc.IsActive)
            .Select(doc => doc.DocumentType)
            .ToListAsync(cancellationToken);

        var missingDocuments = RequiredDocumentTypes
            .Where(required =>
                !existingDocumentTypes.Any(existing =>
                    string.Equals(existing, required, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var experienceStep = await _context.UserRegistrationSteps
            .AsNoTracking()
            .Where(step => step.UserId == userId.Value && step.StepName == ExperienceStepName)
            .OrderByDescending(step => step.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var experience = ExtractExperience(experienceStep?.StepData);
        var hasExperience = !string.IsNullOrWhiteSpace(experience);

        var response = new ServiceProviderMissingDetailsResponse
        {
            UserId = userId.Value,
            BasePincode = basePincode,
            PrimaryPincode = primaryPincode,
            NearbyPincodes = nearbyPincodes,
            SelectedPincodes = selectedPincodes,
            RequiredDocuments = RequiredDocumentTypes.Concat(OptionalDocumentTypes).ToList(),
            MissingDocuments = missingDocuments,
            HasExperience = hasExperience,
            Experience = experience,
            HasPincodePreferences = selectedPincodes.Count > 0
        };

        response.IsComplete = extraInfo?.IsCompleted ?? false;

        return ServiceResult.Ok(response);
    }

    public async Task<ServiceResult> UpdateDetailsAsync(Guid? userId, ServiceProviderDetailsUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (!userId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        if (request == null)
        {
            return ServiceResult.BadRequest("Request body is required.");
        }

        var experience = request.Experience?.Trim();
        if (!string.IsNullOrWhiteSpace(experience))
        {
            await UpsertExperienceAsync(userId.Value, experience, cancellationToken);
        }

        if (request.Pincodes != null && request.Pincodes.Count > 0)
        {
            var normalizedPincodes = request.Pincodes
                .Select(pincode => pincode?.Trim())
                .Where(pincode => !string.IsNullOrWhiteSpace(pincode))
                .Select(pincode => pincode!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalizedPincodes.Count == 0)
            {
                return ServiceResult.BadRequest("At least one valid pincode is required.");
            }

            await UpsertPincodePreferencesAsync(userId.Value, normalizedPincodes, request.PrimaryPincode?.Trim(), cancellationToken);
        }

        var extraInfo = await _context.UsersExtraInfos.FirstOrDefaultAsync(info => info.UserId == userId.Value, cancellationToken);
        if (extraInfo != null)
        {
            var isComplete = await CalculateIsCompletedAsync(userId.Value, cancellationToken);
            if (extraInfo.IsCompleted != isComplete)
            {
                extraInfo.IsCompleted = isComplete;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        // Ensure verification request is created/updated when details are updated
        await EnsureVerificationRequestAsync(userId.Value, cancellationToken);

        return ServiceResult.Ok(new OperationResponse
        {
            Success = true,
            Message = "Details updated successfully."
        });
    }

    public async Task<ServiceResult> UpdateProfileAsync(Guid? userId, ServiceProviderProfileUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (!userId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        if (request == null)
        {
            return ServiceResult.BadRequest("Request body is required.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);
        if (user == null)
        {
            return ServiceResult.NotFound(new MessageResponse { Message = "Service provider not found." });
        }

        var extraInfo = await _context.UsersExtraInfos
            .FirstOrDefaultAsync(info => info.UserId == userId.Value, cancellationToken);

        if (extraInfo == null)
        {
            return ServiceResult.NotFound(new MessageResponse { Message = "Service provider profile not found." });
        }

        var normalizedFullName = request.FullName?.Trim() ?? string.Empty;
        var normalizedPhone = request.PhoneNumber?.Trim() ?? string.Empty;
        var normalizedAlt = request.AlternativeMobile?.Trim() ?? string.Empty;

        user.Name = normalizedFullName;
        user.MobileNumber = normalizedPhone;

        extraInfo.FullName = normalizedFullName;
        extraInfo.PhoneNumber = normalizedPhone;
        extraInfo.AlternativeMobile = normalizedAlt;

        if (string.IsNullOrWhiteSpace(extraInfo.Email))
        {
            extraInfo.Email = user.Email;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok(new OperationResponse
        {
            Success = true,
            Message = "Profile updated successfully."
        });
    }

    public async Task<ServiceResult> UploadDocumentAsync(Guid? userId, string documentType, FileUploadRequest file, CancellationToken cancellationToken = default)
    {
        if (!userId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(documentType))
        {
            return ServiceResult.BadRequest("Document type is required.");
        }

        if (file == null || file.Length == 0)
        {
            return ServiceResult.BadRequest("Document file is required.");
        }

        var normalizedDocumentType = documentType.Trim().ToLowerInvariant();
        var subfolder = $"service-providers/{userId.Value:N}/documents";

        var storageResult = await _fileStorageService.SaveAsync(file, subfolder, cancellationToken);

        var existingDocument = await _context.Documents
            .FirstOrDefaultAsync(doc =>
                doc.UserId == userId.Value &&
                doc.IsActive &&
                doc.DocumentType.ToLower() == normalizedDocumentType,
                cancellationToken);

        if (existingDocument == null)
        {
            existingDocument = new Document
            {
                UserId = userId.Value,
                DocumentType = normalizedDocumentType,
                FileUrl = storageResult.RelativePath,
                FileName = storageResult.OriginalFileName,
                FileSize = storageResult.Size,
                UploadedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.Documents.Add(existingDocument);
        }
        else
        {
            existingDocument.FileUrl = storageResult.RelativePath;
            existingDocument.FileName = storageResult.OriginalFileName;
            existingDocument.FileSize = storageResult.Size;
            existingDocument.UploadedAt = DateTime.UtcNow;
            existingDocument.IsActive = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var extraInfo = await _context.UsersExtraInfos.FirstOrDefaultAsync(info => info.UserId == userId.Value, cancellationToken);
        if (extraInfo != null)
        {
            var isComplete = await CalculateIsCompletedAsync(userId.Value, cancellationToken);
            if (extraInfo.IsCompleted != isComplete)
            {
                extraInfo.IsCompleted = isComplete;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        // Assign admin and create/update verification request when documents are uploaded
        await EnsureVerificationRequestAsync(userId.Value, cancellationToken);

        return ServiceResult.Ok(new DocumentUploadResponse
        {
            DocumentId = existingDocument.Id,
            DocumentType = existingDocument.DocumentType,
            FileUrl = existingDocument.FileUrl,
            FileName = existingDocument.FileName,
            FileSize = existingDocument.FileSize ?? 0,
            UploadedAt = existingDocument.UploadedAt
        });
    }

    private async Task UpsertExperienceAsync(Guid userId, string experience, CancellationToken cancellationToken)
    {
        var step = await _context.UserRegistrationSteps
            .FirstOrDefaultAsync(s => s.UserId == userId && s.StepName == ExperienceStepName, cancellationToken);

        var payload = JsonSerializer.Serialize(new { experience });
        if (step == null)
        {
            step = new UserRegistrationStep
            {
                UserId = userId,
                StepNumber = 3,
                StepName = ExperienceStepName
            };
            _context.UserRegistrationSteps.Add(step);
        }

        step.StepData = payload;
        step.IsCompleted = !string.IsNullOrWhiteSpace(experience);
        step.CompletedAt = step.IsCompleted ? DateTime.UtcNow : null;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertPincodePreferencesAsync(Guid userId, List<string> pincodes, string? primaryPincode, CancellationToken cancellationToken)
    {
        var normalizedPrimary = string.IsNullOrWhiteSpace(primaryPincode) ? null : primaryPincode.Trim();

        var existingPreferences = await _context.ServiceProviderPincodePreferences
            .Where(pref => pref.UserId == userId)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(normalizedPrimary))
        {
            foreach (var preference in existingPreferences)
            {
                preference.IsPrimary = string.Equals(preference.Pincode, normalizedPrimary, StringComparison.OrdinalIgnoreCase);
            }
        }

        foreach (var pincode in pincodes)
        {
            var existing = existingPreferences
                .FirstOrDefault(pref => string.Equals(pref.Pincode, pincode, StringComparison.OrdinalIgnoreCase));

            var isPrimary = !string.IsNullOrWhiteSpace(normalizedPrimary) &&
                            string.Equals(pincode, normalizedPrimary, StringComparison.OrdinalIgnoreCase);

            if (existing == null)
            {
                _context.ServiceProviderPincodePreferences.Add(new ServiceProviderPincodePreference
                {
                    UserId = userId,
                    Pincode = pincode,
                    IsPrimary = isPrimary
                });
            }
            else
            {
                existing.IsPrimary = isPrimary || existing.IsPrimary;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> CalculateIsCompletedAsync(Guid userId, CancellationToken cancellationToken)
    {
        var existingDocumentTypes = await _context.Documents
            .AsNoTracking()
            .Where(doc => doc.UserId == userId && doc.IsActive)
            .Select(doc => doc.DocumentType)
            .ToListAsync(cancellationToken);

        var hasRequiredDocs = RequiredDocumentTypes.All(required =>
            existingDocumentTypes.Any(existing =>
                string.Equals(existing, required, StringComparison.OrdinalIgnoreCase)));

        var experienceStep = await _context.UserRegistrationSteps
            .AsNoTracking()
            .Where(step => step.UserId == userId && step.StepName == ExperienceStepName)
            .OrderByDescending(step => step.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var experience = ExtractExperience(experienceStep?.StepData);
        var hasExperience = !string.IsNullOrWhiteSpace(experience);

        return hasRequiredDocs && hasExperience;
    }

    private static string? ExtractExperience(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.TryGetProperty("experience", out var element))
            {
                return element.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private async Task EnsureVerificationRequestAsync(Guid userId, CancellationToken cancellationToken)
    {
        var address = await _context.UsersAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId && a.IsActive, cancellationToken);

        var pincode = address?.ZipCode?.Trim();
        var assignedAdminId = await ResolveAssignedAdminIdAsync(pincode, cancellationToken);

        var statusId = await GetStatusIdAsync(VerificationStatusCodes.Pending, cancellationToken);
        if (!statusId.HasValue)
        {
            // If PENDING status doesn't exist, skip verification request creation
            return;
        }

        var verification = await _context.ServiceProviderVerifications
            .FirstOrDefaultAsync(v => v.ProviderUserId == userId && v.IsActive, cancellationToken);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
        {
            return;
        }

        if (verification == null)
        {
            // Always assign an admin when creating new verification
            // If no state-specific admin found, ensure default admin is assigned
            var adminIdToAssign = assignedAdminId;
            if (!adminIdToAssign.HasValue)
            {
                // Fallback to default admin if no admin was resolved
                adminIdToAssign = await GetDefaultAdminIdAsync(cancellationToken);
            }

            verification = new ServiceProviderVerification
            {
                ProviderUserId = userId,
                AssignedAdminId = adminIdToAssign,
                VerificationNotes = string.Empty,
                RejectionReason = string.Empty,
                IsActive = true
            };
            _context.ServiceProviderVerifications.Add(verification);
            
            // Set user verification status to pending
            if (user.VerificationStatusId != statusId.Value)
            {
                user.VerificationStatusId = statusId.Value;
            }
        }
        else
        {
            // Update admin assignment if not already assigned
            // This handles the case where verification was created during registration without admin
            if (!verification.AssignedAdminId.HasValue)
            {
                // Ensure we always assign an admin - use resolved admin or fallback to default
                if (assignedAdminId.HasValue)
                {
                    verification.AssignedAdminId = assignedAdminId;
                }
                else
                {
                    // Fallback: explicitly get default admin if no admin was resolved
                    // This ensures default admin is always assigned when no state admin exists
                    var defaultAdminId = await GetDefaultAdminIdAsync(cancellationToken);
                    if (defaultAdminId.HasValue)
                    {
                        verification.AssignedAdminId = defaultAdminId;
                    }
                }
            }

            // If verification was rejected or is in a non-pending state, reset to pending when documents are updated
            if (user.VerificationStatusId != statusId.Value)
            {
                user.VerificationStatusId = statusId.Value;
                verification.VerificationNotes = string.Empty;
                verification.RejectionReason = string.Empty;
                verification.VerifiedAt = null;
                verification.VerifiedBy = null;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<int?> GetStatusIdAsync(string code, CancellationToken cancellationToken)
    {
        return await _context.VerificationStatuses
            .Where(status => status.IsActive && status.Code == code)
            .Select(status => (int?)status.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Guid?> ResolveAssignedAdminIdAsync(string? pincode, CancellationToken cancellationToken)
    {
        var normalizedPincode = (pincode ?? string.Empty).Trim();
        int? stateId = null;

        // Try to find state from pincode
        if (!string.IsNullOrWhiteSpace(normalizedPincode))
        {
            stateId = await _context.CityPincodes
                .AsNoTracking()
                .Include(cp => cp.City)
                .Where(cp =>
                    cp.IsActive &&
                    cp.Pincode == normalizedPincode &&
                    cp.City != null &&
                    cp.City.StateId.HasValue)
                .Select(cp => (int?)cp.City!.StateId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // If state found, try to get admin assigned to that state
        if (stateId.HasValue)
        {
            var adminFromState = await _context.AdminStateAssignments
                .AsNoTracking()
                .Where(assignment => assignment.StateId == stateId.Value)
                .Join(_context.Users.AsNoTracking(),
                    assignment => assignment.AdminUserId,
                    user => user.Id,
                    (assignment, user) => new { assignment, user })
                .Join(_context.Roles.AsNoTracking(),
                    entry => entry.user.RoleId,
                    role => role.Id,
                    (entry, role) => new { entry.assignment, entry.user, role })
                .Where(entry =>
                    entry.user.StatusId == (int)UserStatusEnum.Active &&
                    entry.role.IsActive &&
                    entry.role.Name == RoleNames.Admin)
                .OrderBy(entry => entry.assignment.AssignedAt)
                .Select(entry => (Guid?)entry.user.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (adminFromState.HasValue)
            {
                return adminFromState.Value;
            }
        }

        // If no state-specific admin found, get default admin
        // This should always return an admin if one exists in the system
        var defaultAdminId = await GetDefaultAdminIdAsync(cancellationToken);
        
        // If no default admin exists, this will return null
        // The caller should handle this case, but ideally a default admin should always exist
        return defaultAdminId;
    }

    private async Task<Guid?> GetDefaultAdminIdAsync(CancellationToken cancellationToken)
    {
        // Query for default admin: must be active user with active DefaultAdmin role
        // This should always return an admin if one exists in the system
        // If this returns null, it means no default admin exists or no default admin meets the criteria
        var defaultAdmin = await _context.Users
            .AsNoTracking()
            .Join(_context.Roles.AsNoTracking(),
                user => user.RoleId,
                role => role.Id,
                (user, role) => new { user, role })
            .Where(entry =>
                entry.user.StatusId == (int)UserStatusEnum.Active &&
                entry.role.IsActive &&
                entry.role.Id == (int)RoleEnum.DefaultAdmin)
            .OrderBy(entry => entry.user.CreatedAt)
            .Select(entry => (Guid?)entry.user.Id)
            .FirstOrDefaultAsync(cancellationToken);

        // If no default admin found, this will return null
        // The system should have at least one default admin configured
        return defaultAdmin;
    }

    public async Task<ServiceResult> UpdateServicesAsync(Guid? userId, ServiceProviderServicesUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (!userId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        if (request.ServiceIds == null || request.ServiceIds.Count == 0)
        {
            return ServiceResult.BadRequest("At least one service must be selected.");
        }

        // Verify that all services belong to the selected category
        var services = await _context.Services
            .AsNoTracking()
            .Where(s => request.ServiceIds.Contains(s.Id) && s.IsActive)
            .ToListAsync(cancellationToken);

        if (services.Count != request.ServiceIds.Count)
        {
            return ServiceResult.BadRequest("One or more selected services are invalid or inactive.");
        }

        var invalidServices = services.Where(s => s.CategoryId != request.CategoryId).ToList();
        if (invalidServices.Any())
        {
            return ServiceResult.BadRequest("All selected services must belong to the selected category.");
        }

        // Get all existing provider services for this user (including inactive ones)
        var existingProviderServices = await _context.ProviderServices
            .Where(ps => ps.UserId == userId.Value)
            .ToListAsync(cancellationToken);

        // Deactivate all existing services that are not in the new selection
        var servicesToDeactivate = existingProviderServices
            .Where(ps => !request.ServiceIds.Contains(ps.ServiceId))
            .ToList();

        foreach (var providerService in servicesToDeactivate)
        {
            providerService.IsActive = false;
        }

        // Get existing service IDs (both active and inactive)
        var existingServiceIds = existingProviderServices.Select(ps => ps.ServiceId).ToList();
        
        // Services to add (new services that don't exist yet)
        var servicesToAdd = request.ServiceIds
            .Where(serviceId => !existingServiceIds.Contains(serviceId))
            .ToList();

        foreach (var serviceId in servicesToAdd)
        {
            _context.ProviderServices.Add(new ProviderService
            {
                UserId = userId.Value,
                ServiceId = serviceId,
                Availability = "available", // Default availability
                IsActive = true
            });
        }

        // Reactivate and update existing services that are in the new selection
        var servicesToReactivate = existingProviderServices
            .Where(ps => request.ServiceIds.Contains(ps.ServiceId))
            .ToList();

        foreach (var providerService in servicesToReactivate)
        {
            providerService.IsActive = true;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return ServiceResult.Ok(new { message = "Services updated successfully." });
        }
        catch (Exception ex)
        {
            return ServiceResult.BadRequest($"Failed to update services: {ex.Message}");
        }
    }
}
