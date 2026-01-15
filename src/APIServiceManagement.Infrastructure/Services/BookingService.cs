using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using APIServiceManagement.Application.Interfaces.Services;
using APIServiceManagement.Domain.Entities;
using APIServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Infrastructure.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _context;

    public BookingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult> GetAllAvailableServicesAsync(CancellationToken cancellationToken = default)
    {
        var services = await _context.Services
            .AsNoTracking()
            .Where(service => service.IsActive)
            .Include(service => service.Category)
            .ToListAsync(cancellationToken);

        var response = new ServiceAvailabilityResponse
        {
            Success = true,
            Services = services.Select(service => new ServiceAvailabilityItem
            {
                ServiceId = service.Id.ToString(),
                ServiceName = service.ServiceName,
                Description = service.Description,
                CategoryId = service.CategoryId ?? 0,
                Image = string.IsNullOrWhiteSpace(service.Image) ? null : service.Image,
                Icon = string.IsNullOrWhiteSpace(service.Icon) ? null : service.Icon,
                CategoryImage = service.Category?.Image,
                CategoryIcon = service.Category?.Icon,
                CategoryName = service.Category?.CategoryName ?? "Uncategorized",
                IsAvailable = true,
                BasePrice = 0,
                CalculatedPrice = 0,
                PriceRating = 1
            }).ToList()
        };

        return ServiceResult.Ok(response);
    }

    public async Task<ServiceResult> GetAvailableServicesByPincodeAsync(string pincode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pincode))
        {
            return ServiceResult.BadRequest("Pincode is required.");
        }

        var normalizedPincode = pincode.Trim();
        var pincodeEntry = await _context.CityPincodes
            .AsNoTracking()
            .Include(cp => cp.City)
            .ThenInclude(city => city.State)
            .FirstOrDefaultAsync(cp => cp.IsActive && cp.Pincode == normalizedPincode, cancellationToken);

        if (pincodeEntry == null)
        {
            return ServiceResult.Ok(new ServiceAvailabilityResponse
            {
                Success = false,
                Message = "Pincode not found in our service areas",
                Services = new List<ServiceAvailabilityItem>()
            });
        }

        var serviceMappings = await _context.ServiceAvailablePincodes
            .AsNoTracking()
            .Include(sp => sp.Service)
            .ThenInclude(service => service.Category)
            .Where(sp => sp.IsActive && sp.CityPincodeId == pincodeEntry.Id)
            .ToListAsync(cancellationToken);

        if (serviceMappings.Count == 0)
        {
            return ServiceResult.Ok(new ServiceAvailabilityResponse
            {
                Success = false,
                Message = "No services are currently available in your area. Please try a different pincode.",
                Services = new List<ServiceAvailabilityItem>()
            });
        }

        var response = new ServiceAvailabilityResponse
        {
            Success = true,
            Pincode = normalizedPincode,
            Services = serviceMappings.Select(mapping => new ServiceAvailabilityItem
            {
                ServiceId = mapping.ServiceId?.ToString() ?? string.Empty,
                ServiceName = mapping.Service?.ServiceName ?? string.Empty,
                Description = mapping.Service?.Description,
                CategoryId = mapping.Service?.CategoryId ?? 0,
                Image = mapping.Service?.Image,
                Icon = mapping.Service?.Icon,
                CategoryImage = mapping.Service?.Category?.Image,
                CategoryIcon = mapping.Service?.Category?.Icon,
                CategoryName = mapping.Service?.Category?.CategoryName ?? "Uncategorized",
                IsAvailable = true,
                BasePrice = 0,
                CalculatedPrice = 0,
                PriceRating = mapping.PriceRating ?? 1
            }).ToList(),
            Message = $"{serviceMappings.Count} service(s) available in your area"
        };

        return ServiceResult.Ok(response);
    }

    public async Task<ServiceResult> ValidatePincodeAsync(string pincode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pincode))
        {
            return ServiceResult.BadRequest("Pincode is required.");
        }

        var normalizedPincode = pincode.Trim();
        var pincodeEntry = await _context.CityPincodes
            .AsNoTracking()
            .FirstOrDefaultAsync(cp => cp.IsActive && cp.Pincode == normalizedPincode, cancellationToken);

        if (pincodeEntry == null)
        {
            return ServiceResult.Ok(new PincodeValidationResponse
            {
                Valid = false,
                Message = "Pincode not found in our service areas",
                AvailableServicesCount = 0
            });
        }

        var servicesCount = await _context.ServiceAvailablePincodes
            .AsNoTracking()
            .CountAsync(sp => sp.IsActive && sp.CityPincodeId == pincodeEntry.Id, cancellationToken);

        if (servicesCount == 0)
        {
            return ServiceResult.Ok(new PincodeValidationResponse
            {
                Valid = false,
                Message = "No services available in this pincode",
                AvailableServicesCount = 0
            });
        }

        return ServiceResult.Ok(new PincodeValidationResponse
        {
            Valid = true,
            AvailableServicesCount = servicesCount,
            Message = $"{servicesCount} services available in this pincode"
        });
    }

    public async Task<ServiceResult> CalculateServicePriceAsync(int serviceId, string pincode, CancellationToken cancellationToken = default)
    {
        if (serviceId <= 0 || string.IsNullOrWhiteSpace(pincode))
        {
            return ServiceResult.BadRequest("Service ID and pincode are required.");
        }

        var normalizedPincode = pincode.Trim();
        var pincodeEntry = await _context.CityPincodes
            .AsNoTracking()
            .FirstOrDefaultAsync(cp => cp.IsActive && cp.Pincode == normalizedPincode, cancellationToken);

        if (pincodeEntry == null)
        {
            return ServiceResult.BadRequest("Pincode not found in our service areas.");
        }

        var priceRating = await _context.ServiceAvailablePincodes
            .AsNoTracking()
            .Where(sp => sp.IsActive && sp.ServiceId == serviceId && sp.CityPincodeId == pincodeEntry.Id)
            .Select(sp => sp.PriceRating)
            .FirstOrDefaultAsync(cancellationToken) ?? 1;

        var basePrice = await _context.ServicePrices
            .AsNoTracking()
            .Where(price => price.IsActive && price.ServiceId == serviceId)
            .OrderByDescending(price => price.EffectiveFrom)
            .Select(price => (decimal?)price.Charges)
            .FirstOrDefaultAsync(cancellationToken);

        if (!basePrice.HasValue)
        {
            return ServiceResult.Ok(new ServicePriceCalculationResponse
            {
                ServiceId = serviceId,
                BasePrice = 0,
                PriceRating = priceRating,
                CalculatedPrice = 0
            });
        }

        var calculatedPrice = basePrice.Value * priceRating;

        return ServiceResult.Ok(new ServicePriceCalculationResponse
        {
            ServiceId = serviceId,
            BasePrice = basePrice.Value,
            PriceRating = priceRating,
            CalculatedPrice = calculatedPrice
        });
    }

    public async Task<ServiceResult> CreateBookingAsync(Guid? userId, BookingCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (!userId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Pincode))
        {
            return ServiceResult.BadRequest(new BookingResponse
            {
                Success = false,
                Message = "Pincode is required"
            });
        }

        var normalizedPincode = request.Pincode.Trim();
        var pincodeEntry = await _context.CityPincodes
            .AsNoTracking()
            .FirstOrDefaultAsync(cp => cp.IsActive && cp.Pincode == normalizedPincode, cancellationToken);

        if (pincodeEntry == null)
        {
            return ServiceResult.Ok(new BookingResponse
            {
                Success = false,
                Message = "Pincode not found in our service areas"
            });
        }

        var serviceMapping = await _context.ServiceAvailablePincodes
            .AsNoTracking()
            .FirstOrDefaultAsync(sp =>
                sp.IsActive &&
                sp.CityPincodeId == pincodeEntry.Id &&
                sp.ServiceId == request.ServiceId, cancellationToken);

        if (serviceMapping == null)
        {
            return ServiceResult.Ok(new BookingResponse
            {
                Success = false,
                Message = "This service is not available in the specified pincode"
            });
        }

        var basePrice = await _context.ServicePrices
            .AsNoTracking()
            .Where(price => price.IsActive && price.ServiceId == request.ServiceId)
            .OrderByDescending(price => price.EffectiveFrom)
            .Select(price => (decimal?)price.Charges)
            .FirstOrDefaultAsync(cancellationToken);

        var priceRating = serviceMapping.PriceRating ?? 1;
        var estimatedPrice = basePrice.HasValue ? basePrice.Value * priceRating : (decimal?)null;

        var booking = new BookingRequest
        {
            CustomerId = userId.Value,
            ServiceId = request.ServiceId,
            Pincode = normalizedPincode,
            Status = "pending",
            RequestDescription = request.RequestDescription ?? string.Empty,
            CustomerAddress = request.CustomerAddress ?? string.Empty,
            CustomerPhone = request.CustomerPhone ?? string.Empty,
            CustomerName = request.CustomerName ?? string.Empty,
            PreferredDate = request.PreferredDate,
            PreferredTime = request.PreferredTime,
            EstimatedPrice = estimatedPrice,
            WorkingHours = request.WorkingHours ?? 1
        };

        _context.BookingRequests.Add(booking);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok(new BookingResponse
        {
            Success = true,
            Booking = await BuildBookingDtoAsync(booking, cancellationToken),
            Message = "Booking request created successfully"
        });
    }

    public async Task<ServiceResult> GetBookingRequestsAsync(
        string? status,
        string? pincode,
        int? serviceId,
        Guid? customerId,
        Guid? serviceProviderId,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? sortBy,
        string? sortOrder,
        int page,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = _context.BookingRequests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statuses = status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (statuses.Length > 0)
            {
                query = query.Where(b => statuses.Contains(b.Status));
            }
        }

        if (!string.IsNullOrWhiteSpace(pincode))
        {
            query = query.Where(b => b.Pincode == pincode);
        }

        if (serviceId.HasValue)
        {
            query = query.Where(b => b.ServiceId == serviceId.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(b => b.CustomerId == customerId.Value);
        }

        if (serviceProviderId.HasValue)
        {
            query = query.Where(b => b.ServiceProviderId == serviceProviderId.Value);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(b => b.CreatedAt >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(b => b.CreatedAt <= dateTo.Value);
        }

        var ordering = (sortBy ?? "created_at").ToLowerInvariant();
        var ascending = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);

        query = ordering switch
        {
            "status" => ascending ? query.OrderBy(b => b.Status) : query.OrderByDescending(b => b.Status),
            "preferred_date" => ascending ? query.OrderBy(b => b.PreferredDate) : query.OrderByDescending(b => b.PreferredDate),
            _ => ascending ? query.OrderBy(b => b.CreatedAt) : query.OrderByDescending(b => b.CreatedAt)
        };

        var skip = Math.Max(page - 1, 0) * Math.Max(limit, 1);
        var bookings = await query.Skip(skip).Take(limit).ToListAsync(cancellationToken);

        var result = new List<BookingRequestDto>();
        foreach (var booking in bookings)
        {
            result.Add(await BuildBookingDtoAsync(booking, cancellationToken));
        }

        return ServiceResult.Ok(result);
    }

    public async Task<ServiceResult> AssignServiceProviderAsync(Guid? adminId, BookingAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        if (!adminId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        var booking = await _context.BookingRequests.FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);
        if (booking == null)
        {
            return ServiceResult.Ok(new BookingResponse { Success = false, Message = "Booking not found" });
        }

        booking.AdminId = adminId.Value;
        booking.AdminNotes = request.AdminNotes ?? booking.AdminNotes;
        booking.EstimatedPrice = request.EstimatedPrice ?? booking.EstimatedPrice;
        booking.UpdatedAt = DateTime.UtcNow;

        if (string.Equals(request.Status, "rejected", StringComparison.OrdinalIgnoreCase))
        {
            booking.Status = "rejected";
        }
        else
        {
            booking.ServiceProviderId = request.ServiceProviderId;
            booking.Status = string.IsNullOrWhiteSpace(request.Status) ? "assigned" : request.Status;
            booking.AssignedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok(new BookingResponse
        {
            Success = true,
            Booking = await BuildBookingDtoAsync(booking, cancellationToken),
            Message = "Booking updated successfully"
        });
    }

    public async Task<ServiceResult> UpdateBookingStatusAsync(Guid bookingId, BookingUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var booking = await _context.BookingRequests.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking == null)
        {
            return ServiceResult.Ok(new BookingResponse { Success = false, Message = "Booking not found" });
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            booking.Status = request.Status;
        }

        booking.ServiceProviderNotes = request.ServiceProviderNotes ?? booking.ServiceProviderNotes;
        booking.FinalPrice = request.FinalPrice ?? booking.FinalPrice;
        booking.CustomerRating = request.CustomerRating ?? booking.CustomerRating;
        booking.CustomerFeedback = request.CustomerFeedback ?? booking.CustomerFeedback;
        booking.PreferredDate = request.PreferredDate ?? booking.PreferredDate;
        booking.PreferredTime = request.PreferredTime ?? booking.PreferredTime;
        booking.WorkingHours = request.WorkingHours ?? booking.WorkingHours;
        booking.StartedAt = request.StartedAt ?? booking.StartedAt;
        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok(new BookingResponse
        {
            Success = true,
            Booking = await BuildBookingDtoAsync(booking, cancellationToken),
            Message = "Booking status updated successfully"
        });
    }

    public async Task<ServiceResult> GetCustomerDashboardAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        if (!userId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        var primaryPincode = await _context.UserPincodePreferences
            .AsNoTracking()
            .Where(pref => pref.UserId == userId.Value && pref.IsPrimary)
            .OrderByDescending(pref => pref.UpdatedAt)
            .Select(pref => pref.Pincode)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(primaryPincode))
        {
            return ServiceResult.Ok(new CustomerDashboardResponse());
        }

        var servicesResponse = await BuildServiceAvailabilityResponseAsync(primaryPincode, cancellationToken);
        var recentBookings = await _context.BookingRequests
            .AsNoTracking()
            .Where(b => b.CustomerId == userId.Value)
            .OrderByDescending(b => b.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        var totalBookings = await _context.BookingRequests
            .AsNoTracking()
            .CountAsync(b => b.CustomerId == userId.Value, cancellationToken);

        var pendingBookings = await _context.BookingRequests
            .AsNoTracking()
            .CountAsync(b => b.CustomerId == userId.Value && (b.Status == "pending" || b.Status == "assigned"), cancellationToken);

        var completedBookings = await _context.BookingRequests
            .AsNoTracking()
            .CountAsync(b => b.CustomerId == userId.Value && b.Status == "completed", cancellationToken);

        var dashboard = new CustomerDashboardResponse
        {
            UserPincode = primaryPincode,
            AvailableServices = servicesResponse.Services,
            RecentBookings = new List<BookingRequestDto>(),
            TotalBookings = totalBookings,
            PendingBookings = pendingBookings,
            CompletedBookings = completedBookings
        };

        foreach (var booking in recentBookings)
        {
            dashboard.RecentBookings.Add(await BuildBookingDtoAsync(booking, cancellationToken));
        }

        return ServiceResult.Ok(dashboard);
    }

    public async Task<ServiceResult> GetAdminDashboardAsync(Guid? adminId, CancellationToken cancellationToken = default)
    {
        if (!adminId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        var stateIds = await _context.AdminStateAssignments
            .AsNoTracking()
            .Where(assignment => assignment.AdminUserId == adminId.Value )
            .Select(assignment => assignment.StateId)
            .ToListAsync(cancellationToken);

        if (stateIds.Count == 0)
        {
            return ServiceResult.Ok(new AdminBookingDashboardResponse());
        }

        var pincodes = await _context.CityPincodes
            .AsNoTracking()
            .Include(cp => cp.City)
            .Where(cp => cp.IsActive && cp.City != null && cp.City.StateId.HasValue && stateIds.Contains(cp.City.StateId.Value))
            .Select(cp => cp.Pincode)
            .ToListAsync(cancellationToken);

        if (pincodes.Count == 0)
        {
            return ServiceResult.Ok(new AdminBookingDashboardResponse());
        }

        var bookingsQuery = _context.BookingRequests.AsNoTracking().Where(b => pincodes.Contains(b.Pincode));

        var totalRequests = await bookingsQuery.CountAsync(cancellationToken);
        var pendingRequests = await bookingsQuery.CountAsync(b => b.Status == "pending", cancellationToken);
        var assignedRequests = await bookingsQuery.CountAsync(b => b.Status == "assigned", cancellationToken);
        var inProgressRequests = await bookingsQuery.CountAsync(b => b.Status == "in_progress", cancellationToken);
        var holdRequests = await bookingsQuery.CountAsync(b => b.Status == "on_hold", cancellationToken);
        var completedRequests = await bookingsQuery.CountAsync(b => b.Status == "completed", cancellationToken);

        var recentRequests = await bookingsQuery
            .OrderByDescending(b => b.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        var pincodeStats = new List<PincodeStatResponse>();
        foreach (var pincode in pincodes.Distinct())
        {
            var total = await _context.BookingRequests
                .AsNoTracking()
                .CountAsync(b => b.Pincode == pincode, cancellationToken);

            var pending = await _context.BookingRequests
                .AsNoTracking()
                .CountAsync(b => b.Pincode == pincode && b.Status == "pending", cancellationToken);

            pincodeStats.Add(new PincodeStatResponse
            {
                Pincode = pincode,
                TotalRequests = total,
                PendingRequests = pending
            });
        }

        var response = new AdminBookingDashboardResponse
        {
            TotalRequests = totalRequests,
            PendingRequests = pendingRequests,
            AssignedRequests = assignedRequests,
            InProgressRequests = inProgressRequests,
            HoldRequests = holdRequests,
            CompletedRequests = completedRequests,
            RecentRequests = new List<BookingRequestDto>(),
            PincodeStats = pincodeStats
        };

        foreach (var booking in recentRequests)
        {
            response.RecentRequests.Add(await BuildBookingDtoAsync(booking, cancellationToken));
        }

        return ServiceResult.Ok(response);
    }

    public async Task<ServiceResult> GetServiceProviderDashboardAsync(Guid? providerId, CancellationToken cancellationToken = default)
    {
        if (!providerId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        var assignedBookings = await _context.BookingRequests
            .AsNoTracking()
            .Where(b => b.ServiceProviderId == providerId.Value && (b.Status == "assigned" || b.Status == "in_progress"))
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        var completedBookings = await _context.BookingRequests
            .AsNoTracking()
            .Where(b => b.ServiceProviderId == providerId.Value && b.Status == "completed")
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        var totalAssignments = await _context.BookingRequests
            .AsNoTracking()
            .CountAsync(b => b.ServiceProviderId == providerId.Value, cancellationToken);

        var pendingAssignments = await _context.BookingRequests
            .AsNoTracking()
            .CountAsync(b => b.ServiceProviderId == providerId.Value && (b.Status == "assigned" || b.Status == "in_progress"), cancellationToken);

        var completedAssignments = await _context.BookingRequests
            .AsNoTracking()
            .CountAsync(b => b.ServiceProviderId == providerId.Value && b.Status == "completed", cancellationToken);

        var response = new ServiceProviderDashboardResponse
        {
            AssignedBookings = new List<BookingRequestDto>(),
            CompletedBookings = new List<BookingRequestDto>(),
            TotalAssignments = totalAssignments,
            PendingAssignments = pendingAssignments,
            CompletedAssignments = completedAssignments
        };

        foreach (var booking in assignedBookings)
        {
            response.AssignedBookings.Add(await BuildBookingDtoAsync(booking, cancellationToken));
        }

        foreach (var booking in completedBookings)
        {
            response.CompletedBookings.Add(await BuildBookingDtoAsync(booking, cancellationToken));
        }

        return ServiceResult.Ok(response);
    }

    public async Task<ServiceResult> GetUserPincodePreferencesAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        if (!userId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        var preferences = await _context.UserPincodePreferences
            .AsNoTracking()
            .Where(pref => pref.UserId == userId.Value)
            .OrderByDescending(pref => pref.IsPrimary)
            .ThenByDescending(pref => pref.CreatedAt)
            .ToListAsync(cancellationToken);

        var response = preferences.Select(pref => new UserPincodePreferenceResponse
        {
            Id = pref.Id,
            UserId = pref.UserId,
            Pincode = pref.Pincode,
            IsPrimary = pref.IsPrimary,
            CreatedAt = pref.CreatedAt,
            UpdatedAt = pref.UpdatedAt
        }).ToList();

        return ServiceResult.Ok(response);
    }

    public async Task<ServiceResult> SaveUserPincodePreferenceAsync(Guid? userId, PincodePreferenceRequest request, CancellationToken cancellationToken = default)
    {
        if (!userId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        var normalizedPincode = request.Pincode?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedPincode))
        {
            return ServiceResult.BadRequest("Pincode is required.");
        }

        if (request.IsPrimary)
        {
            var existingPrimaries = await _context.UserPincodePreferences
                .Where(pref => pref.UserId == userId.Value && pref.IsPrimary)
                .ToListAsync(cancellationToken);

            foreach (var preference in existingPrimaries)
            {
                preference.IsPrimary = false;
                preference.UpdatedAt = DateTime.UtcNow;
            }
        }

        var existingPreference = await _context.UserPincodePreferences
            .FirstOrDefaultAsync(pref => pref.UserId == userId.Value && pref.Pincode == normalizedPincode, cancellationToken);

        if (existingPreference != null)
        {
            existingPreference.IsPrimary = request.IsPrimary;
            existingPreference.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.UserPincodePreferences.Add(new UserPincodePreference
            {
                UserId = userId.Value,
                Pincode = normalizedPincode,
                IsPrimary = request.IsPrimary
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok(new OperationResponse { Success = true });
    }

    private async Task<ServiceAvailabilityResponse> BuildServiceAvailabilityResponseAsync(string pincode, CancellationToken cancellationToken)
    {
        var response = new ServiceAvailabilityResponse
        {
            Success = false,
            Services = new List<ServiceAvailabilityItem>()
        };

        var pincodeEntry = await _context.CityPincodes
            .AsNoTracking()
            .FirstOrDefaultAsync(cp => cp.IsActive && cp.Pincode == pincode, cancellationToken);

        if (pincodeEntry == null)
        {
            return response;
        }

        var serviceMappings = await _context.ServiceAvailablePincodes
            .AsNoTracking()
            .Include(sp => sp.Service)
            .ThenInclude(service => service.Category)
            .Where(sp => sp.IsActive && sp.CityPincodeId == pincodeEntry.Id)
            .ToListAsync(cancellationToken);

        if (serviceMappings.Count == 0)
        {
            return response;
        }

        response.Success = true;
        response.Pincode = pincode;
        response.Services = serviceMappings.Select(mapping => new ServiceAvailabilityItem
        {
            ServiceId = mapping.ServiceId?.ToString() ?? string.Empty,
            ServiceName = mapping.Service?.ServiceName ?? string.Empty,
            Description = mapping.Service?.Description,
            CategoryId = mapping.Service?.CategoryId ?? 0,
            Image = mapping.Service?.Image,
            Icon = mapping.Service?.Icon,
            CategoryImage = mapping.Service?.Category?.Image,
            CategoryIcon = mapping.Service?.Category?.Icon,
            CategoryName = mapping.Service?.Category?.CategoryName ?? "Uncategorized",
            IsAvailable = true,
            BasePrice = 0,
            CalculatedPrice = 0,
            PriceRating = mapping.PriceRating ?? 1
        }).ToList();

        return response;
    }

    private async Task<BookingRequestDto> BuildBookingDtoAsync(BookingRequest booking, CancellationToken cancellationToken)
    {
        var service = await _context.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == booking.ServiceId, cancellationToken);

        var userIds = new List<Guid> { booking.CustomerId };
        if (booking.ServiceProviderId.HasValue)
        {
            userIds.Add(booking.ServiceProviderId.Value);
        }
        if (booking.AdminId.HasValue)
        {
            userIds.Add(booking.AdminId.Value);
        }

        var usersExtra = await _context.UsersExtraInfos
            .AsNoTracking()
            .Where(info => info.UserId.HasValue && userIds.Contains(info.UserId.Value))
            .ToListAsync(cancellationToken);

        BookingUserDto? MapUser(Guid userId)
        {
            var info = usersExtra.FirstOrDefault(u => u.UserId == userId);
            return info == null
                ? new BookingUserDto { Id = userId }
                : new BookingUserDto
                {
                    Id = userId,
                    Name = info.FullName,
                    Email = info.Email,
                    Phone = info.PhoneNumber
                };
        }

        BookingAdminDto? MapAdmin(Guid adminId)
        {
            var info = usersExtra.FirstOrDefault(u => u.UserId == adminId);
            return info == null
                ? new BookingAdminDto { Id = adminId }
                : new BookingAdminDto
                {
                    Id = adminId,
                    Name = info.FullName,
                    Email = info.Email
                };
        }

        return new BookingRequestDto
        {
            Id = booking.Id,
            CustomerId = booking.CustomerId,
            ServiceId = booking.ServiceId,
            Pincode = booking.Pincode,
            ServiceProviderId = booking.ServiceProviderId,
            AdminId = booking.AdminId,
            Status = booking.Status,
            RequestDescription = booking.RequestDescription,
            CustomerAddress = booking.CustomerAddress,
            CustomerPhone = booking.CustomerPhone,
            CustomerName = booking.CustomerName,
            PreferredDate = booking.PreferredDate,
            PreferredTime = booking.PreferredTime,
            EstimatedPrice = booking.EstimatedPrice,
            FinalPrice = booking.FinalPrice,
            AdminNotes = booking.AdminNotes,
            ServiceProviderNotes = booking.ServiceProviderNotes,
            CustomerRating = booking.CustomerRating,
            CustomerFeedback = booking.CustomerFeedback,
            AssignedAt = booking.AssignedAt,
            StartedAt = booking.StartedAt,
            CompletedAt = booking.CompletedAt,
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt,
            WorkingHours = booking.WorkingHours,
            Service = service == null
                ? null
                : new BookingServiceDto
                {
                    Id = service.Id,
                    ServiceName = service.ServiceName,
                    Description = service.Description,
                    CategoryId = service.CategoryId ?? 0
                },
            Customer = MapUser(booking.CustomerId),
            ServiceProvider = booking.ServiceProviderId.HasValue
                ? new BookingServiceProviderDto
                {
                    Id = booking.ServiceProviderId.Value,
                    Name = usersExtra.FirstOrDefault(u => u.UserId == booking.ServiceProviderId.Value)?.FullName,
                    Email = usersExtra.FirstOrDefault(u => u.UserId == booking.ServiceProviderId.Value)?.Email,
                    Phone = usersExtra.FirstOrDefault(u => u.UserId == booking.ServiceProviderId.Value)?.PhoneNumber
                }
                : null,
            Admin = booking.AdminId.HasValue ? MapAdmin(booking.AdminId.Value) : null
        };
    }
}
