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
using System.Net;
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

    private async Task<int> GetStatusIdByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var status = await _context.BookingStatuses
            .AsNoTracking()
            .Where(s => s.Code == code && s.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (status == null)
        {
            throw new InvalidOperationException($"Booking status with code '{code}' not found.");
        }
        
        return status.Id;
    }

    private async Task<Dictionary<string, int>> GetStatusIdsByCodesAsync(string[] codes, CancellationToken cancellationToken = default)
    {
        var normalizedCodes = codes.Select(c => c.ToUpperInvariant()).ToArray();
        var statuses = await _context.BookingStatuses
            .AsNoTracking()
            .Where(s => normalizedCodes.Contains(s.Code) && s.IsActive)
            .ToListAsync(cancellationToken);
        
        return statuses.ToDictionary(s => s.Code, s => s.Id);
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
            .ThenInclude(city => city!.State)
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

        var serviceIdsInPincode = await _context.ServiceProviderPincodePreferences
            .AsNoTracking()
            .Where(pref => pref.Pincode == normalizedPincode)
            .Join(_context.ProviderServices.AsNoTracking().Where(ps => ps.IsActive),
                pref => pref.UserId,
                ps => ps.UserId,
                (pref, ps) => ps.ServiceId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (serviceIdsInPincode.Count == 0)
        {
            return ServiceResult.Ok(new ServiceAvailabilityResponse
            {
                Success = false,
                Message = "No services are currently available in your area. Please try a different pincode.",
                Services = new List<ServiceAvailabilityItem>()
            });
        }

        var services = await _context.Services
            .AsNoTracking()
            .Include(s => s.Category)
            .Where(s => s.IsActive && serviceIdsInPincode.Contains(s.Id))
            .ToListAsync(cancellationToken);

        var response = new ServiceAvailabilityResponse
        {
            Success = true,
            Pincode = normalizedPincode,
            Services = services.Select(service => new ServiceAvailabilityItem
            {
                ServiceId = service.Id.ToString(),
                ServiceName = service.ServiceName,
                Description = service.Description,
                CategoryId = service.CategoryId ?? 0,
                Image = service.Image,
                Icon = service.Icon,
                CategoryImage = service.Category?.Image,
                CategoryIcon = service.Category?.Icon,
                CategoryName = service.Category?.CategoryName ?? "Uncategorized",
                IsAvailable = true,
                BasePrice = 0,
                CalculatedPrice = 0,
                PriceRating = 1 // Default price rating
            }).ToList(),
            Message = $"{services.Count} service(s) available in your area"
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

        

        return ServiceResult.Ok(new PincodeValidationResponse
        {
            Valid = true,
            AvailableServicesCount = 0,
            Message = $"now services available in this pincode"
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

        var isAvailable = await _context.ServiceProviderPincodePreferences
            .AsNoTracking()
            .Where(pref => pref.Pincode == normalizedPincode)
            .Join(_context.ProviderServices.AsNoTracking().Where(ps => ps.IsActive && ps.ServiceId == serviceId),
                pref => pref.UserId,
                ps => ps.UserId,
                (pref, ps) => ps.ServiceId)
            .AnyAsync(cancellationToken);

        if (!isAvailable)
        {
            return ServiceResult.BadRequest("This service is not available in the specified pincode.");
        }

        var priceRating = 1; // Default price rating since ServiceAvailablePincodes is removed

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

        var isAvailable = await _context.ServiceProviderPincodePreferences
            .AsNoTracking()
            .Where(pref => pref.Pincode == normalizedPincode)
            .Join(_context.ProviderServices.AsNoTracking().Where(ps => ps.IsActive && ps.ServiceId == request.ServiceId),
                pref => pref.UserId,
                ps => ps.UserId,
                (pref, ps) => ps.ServiceId)
            .AnyAsync(cancellationToken);

        if (!isAvailable)
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

        var priceRating = 1; // Default price rating
        var estimatedPrice = basePrice.HasValue ? basePrice.Value * priceRating : (decimal?)null;

        // Build address from separate fields
        var addressParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.AddressLine1))
            addressParts.Add(request.AddressLine1.Trim());
        if (!string.IsNullOrWhiteSpace(request.AddressLine2))
            addressParts.Add(request.AddressLine2.Trim());
        if (!string.IsNullOrWhiteSpace(request.City))
            addressParts.Add(request.City.Trim());
        if (!string.IsNullOrWhiteSpace(request.State))
            addressParts.Add(request.State.Trim());
        var fullAddress = string.Join(", ", addressParts);

        // Convert PreferredDate to UTC if provided
        // For date-only values, we want to preserve the date but ensure it's UTC
        DateTime? preferredDateUtc = null;
        if (request.PreferredDate.HasValue)
        {
            var preferredDate = request.PreferredDate.Value;
            // If DateTime is Unspecified, treat as UTC (common when deserializing from JSON)
            // If Local, convert to UTC
            if (preferredDate.Kind == DateTimeKind.Unspecified)
            {
                // For date-only values, assume they're already in UTC
                preferredDateUtc = DateTime.SpecifyKind(preferredDate, DateTimeKind.Utc);
            }
            else if (preferredDate.Kind == DateTimeKind.Local)
            {
                preferredDateUtc = preferredDate.ToUniversalTime();
            }
            else
            {
                preferredDateUtc = preferredDate;
            }
        }

        var pendingStatusId = await GetStatusIdByCodeAsync(BookingStatusCodes.Pending, cancellationToken);

        // Fetch customer name from UsersExtraInfos
        var customerInfo = await _context.UsersExtraInfos
            .AsNoTracking()
            .FirstOrDefaultAsync(info => info.UserId == userId.Value, cancellationToken);
        
        var customerName = !string.IsNullOrWhiteSpace(customerInfo?.FullName) 
            ? customerInfo.FullName.Trim() 
            : string.Empty;
        
        // Ensure customer name is not empty (required by database constraint)
        if (string.IsNullOrWhiteSpace(customerName))
        {
            return ServiceResult.BadRequest(new BookingResponse
            {
                Success = false,
                Message = "Customer name is required. Please update your profile with your full name."
            });
        }

        var booking = new BookingRequest
        {
            CustomerId = userId.Value,
            ServiceId = request.ServiceId,
            ServiceTypeId = request.ServiceTypeId,
            Pincode = normalizedPincode,
            StatusId = pendingStatusId,
            Status = BookingStatusStrings.Pending, // Backward compatibility
            RequestDescription = request.RequestDescription ?? string.Empty,
            CustomerAddress = fullAddress,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            State = request.State,
            CustomerPhone = request.CustomerPhone ?? string.Empty,
            AlternativeMobileNumber = request.AlternativeMobileNumber,
            CustomerName = customerName,
            PreferredDate = preferredDateUtc,
            TimeSlot = request.TimeSlot,
            EstimatedPrice = estimatedPrice,
            WorkingHours = request.WorkingHours ?? 1,
            // Set default values for NOT NULL string columns
            AdminNotes = string.Empty,
            ServiceProviderNotes = string.Empty,
            CustomerFeedback = string.Empty
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
        try
        {
            IQueryable<BookingRequest> query = _context.BookingRequests.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(status))
            {
                var statusCodes = status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => s.ToUpperInvariant())
                    .ToList();
                
                if (statusCodes.Count > 0)
                {
                    var statusIds = await _context.BookingStatuses
                        .AsNoTracking()
                        .Where(s => statusCodes.Contains(s.Code) && s.IsActive)
                        .Select(s => s.Id)
                        .ToListAsync(cancellationToken);
                    
                    if (statusIds.Count > 0)
                    {
                        query = query.Where(b => statusIds.Contains(b.StatusId));
                    }
                    else
                    {
                        // No matching statuses found, return empty result
                        return ServiceResult.Ok(new List<BookingRequestDto>());
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(pincode))
            {
                var normalizedPincode = pincode.Trim();
                query = query.Where(b => b.Pincode == normalizedPincode);
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
                var dateFromUtc = dateFrom.Value.Kind == DateTimeKind.Unspecified 
                    ? DateTime.SpecifyKind(dateFrom.Value, DateTimeKind.Utc)
                    : dateFrom.Value.ToUniversalTime();
                query = query.Where(b => b.CreatedAt >= dateFromUtc);
            }

            if (dateTo.HasValue)
            {
                var dateToUtc = dateTo.Value.Kind == DateTimeKind.Unspecified 
                    ? DateTime.SpecifyKind(dateTo.Value, DateTimeKind.Utc)
                    : dateTo.Value.ToUniversalTime();
                // Include the entire day by adding one day and using < instead of <=
                query = query.Where(b => b.CreatedAt < dateToUtc.AddDays(1));
            }

            var ordering = (sortBy ?? SortFieldNames.CreatedAt).ToLowerInvariant();
            var ascending = string.Equals(sortOrder, SortOrder.Ascending, StringComparison.OrdinalIgnoreCase);

            IOrderedQueryable<BookingRequest> orderedQuery = ordering switch
            {
                var o when o == SortFieldNames.Status => ascending 
                    ? query.OrderBy(b => b.StatusId) 
                    : query.OrderByDescending(b => b.StatusId),
                var o when o == SortFieldNames.PreferredDate => ascending 
                    ? query.OrderBy(b => b.PreferredDate ?? DateTime.MaxValue) 
                    : query.OrderByDescending(b => b.PreferredDate ?? DateTime.MinValue),
                _ => ascending 
                    ? query.OrderBy(b => b.CreatedAt) 
                    : query.OrderByDescending(b => b.CreatedAt)
            };

            // Ensure page and limit are valid
            page = Math.Max(1, page);
            limit = Math.Max(1, Math.Min(limit, 100)); // Cap limit at 100 to prevent excessive queries
            var skip = (page - 1) * limit;
            
            var bookings = await orderedQuery
                .Skip(skip)
                .Take(limit)
                .ToListAsync(cancellationToken);

            if (bookings.Count == 0)
            {
                return ServiceResult.Ok(new List<BookingRequestDto>());
            }

            // Batch load all related data to avoid N+1 queries
            var serviceIds = bookings.Select(b => b.ServiceId).Distinct().ToList();
            var userIds = bookings
                .SelectMany(b => new[] { b.CustomerId }
                    .Concat(b.ServiceProviderId.HasValue ? new[] { b.ServiceProviderId.Value } : Enumerable.Empty<Guid>())
                    .Concat(b.AdminId.HasValue ? new[] { b.AdminId.Value } : Enumerable.Empty<Guid>()))
                .Distinct()
                .ToList();
            var serviceTypeIds = bookings
                .Where(b => b.ServiceTypeId.HasValue)
                .Select(b => b.ServiceTypeId!.Value)
                .Distinct()
                .ToList();

            Dictionary<int, Service> services;
            if (serviceIds.Count > 0)
            {
                services = await _context.Services
                    .AsNoTracking()
                    .Where(s => serviceIds.Contains(s.Id))
                    .ToDictionaryAsync(s => s.Id, cancellationToken);
            }
            else
            {
                services = new Dictionary<int, Service>();
            }

            List<UsersExtraInfo> usersExtra;
            if (userIds.Count > 0)
            {
                usersExtra = await _context.UsersExtraInfos
                    .AsNoTracking()
                    .Where(info => info.UserId.HasValue && userIds.Contains(info.UserId.Value))
                    .ToListAsync(cancellationToken);
            }
            else
            {
                usersExtra = new List<UsersExtraInfo>();
            }

            var userExtraDict = usersExtra
                .Where(u => u.UserId.HasValue)
                .GroupBy(u => u.UserId!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            Dictionary<int, ServiceType> serviceTypes;
            if (serviceTypeIds.Count > 0)
            {
                serviceTypes = await _context.ServiceTypes
                    .AsNoTracking()
                    .Where(st => serviceTypeIds.Contains(st.Id))
                    .ToDictionaryAsync(st => st.Id, cancellationToken);
            }
            else
            {
                serviceTypes = new Dictionary<int, ServiceType>();
            }

            var result = new List<BookingRequestDto>();
            foreach (var booking in bookings)
            {
                try
                {
                    result.Add(BuildBookingDto(booking, services, userExtraDict, serviceTypes));
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing other bookings
                    // In production, you might want to log this to a logging service
                    System.Diagnostics.Debug.WriteLine($"Error building DTO for booking {booking.Id}: {ex.Message}");
                    // Continue with next booking
                }
            }

            return ServiceResult.Ok(result);
        }
        catch (Exception ex)
        {
            // Log the exception (in production, use proper logging)
            System.Diagnostics.Debug.WriteLine($"Error in GetBookingRequestsAsync: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // Return a generic error message
            return new ServiceResult
            {
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
                Payload = new { Message = "An error occurred while retrieving booking requests. Please try again later." }
            };
        }
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

        if (string.Equals(request.Status, VerificationStatusStrings.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            var rejectedStatusId = await GetStatusIdByCodeAsync(BookingStatusCodes.Rejected, cancellationToken);
            booking.StatusId = rejectedStatusId;
            booking.Status = BookingStatusStrings.Rejected; // Backward compatibility
        }
        else
        {
            booking.ServiceProviderId = request.ServiceProviderId;
            var statusCode = string.IsNullOrWhiteSpace(request.Status) ? BookingStatusCodes.Assigned : request.Status.ToUpperInvariant();
            var statusId = await GetStatusIdByCodeAsync(statusCode, cancellationToken);
            booking.StatusId = statusId;
            booking.Status = request.Status ?? BookingStatusStrings.Assigned; // Backward compatibility
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
            var statusCode = request.Status.ToUpperInvariant();
            var statusId = await GetStatusIdByCodeAsync(statusCode, cancellationToken);
            booking.StatusId = statusId;
            booking.Status = request.Status; // Backward compatibility
        }

        booking.ServiceProviderNotes = request.ServiceProviderNotes ?? booking.ServiceProviderNotes;
        booking.FinalPrice = request.FinalPrice ?? booking.FinalPrice;
        booking.CustomerRating = request.CustomerRating ?? booking.CustomerRating;
        booking.CustomerFeedback = request.CustomerFeedback ?? booking.CustomerFeedback;
        // Convert PreferredDate to UTC if provided
        if (request.PreferredDate.HasValue)
        {
            var preferredDate = request.PreferredDate.Value;
            if (preferredDate.Kind == DateTimeKind.Unspecified)
            {
                booking.PreferredDate = DateTime.SpecifyKind(preferredDate, DateTimeKind.Utc);
            }
            else if (preferredDate.Kind == DateTimeKind.Local)
            {
                booking.PreferredDate = preferredDate.ToUniversalTime();
            }
            else
            {
                booking.PreferredDate = preferredDate;
            }
        }
        booking.PreferredTime = request.PreferredTime ?? booking.PreferredTime;
        booking.WorkingHours = request.WorkingHours ?? booking.WorkingHours;
        // Convert StartedAt to UTC if provided
        if (request.StartedAt.HasValue)
        {
            var startedAt = request.StartedAt.Value;
            if (startedAt.Kind == DateTimeKind.Unspecified)
            {
                booking.StartedAt = DateTime.SpecifyKind(startedAt, DateTimeKind.Utc);
            }
            else if (startedAt.Kind == DateTimeKind.Local)
            {
                booking.StartedAt = startedAt.ToUniversalTime();
            }
            else
            {
                booking.StartedAt = startedAt;
            }
        }
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

        var primaryPincode = await _context.CustomerPincodePreferences
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

        var statusIds = await GetStatusIdsByCodesAsync(new[] { 
            BookingStatusCodes.Pending, 
            BookingStatusCodes.Assigned, 
            BookingStatusCodes.Completed 
        }, cancellationToken);
        var pendingStatusIds = new[] { 
            statusIds.GetValueOrDefault(BookingStatusCodes.Pending), 
            statusIds.GetValueOrDefault(BookingStatusCodes.Assigned) 
        }.Where(id => id > 0).ToList();
        var completedStatusId = statusIds.GetValueOrDefault(BookingStatusCodes.Completed);

        var pendingBookings = await _context.BookingRequests
            .AsNoTracking()
            .CountAsync(b => b.CustomerId == userId.Value && pendingStatusIds.Contains(b.StatusId), cancellationToken);

        var completedBookings = await _context.BookingRequests
            .AsNoTracking()
            .CountAsync(b => b.CustomerId == userId.Value && b.StatusId == completedStatusId, cancellationToken);

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

        // Verify user has admin role
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == adminId.Value, cancellationToken);

        if (user == null)
        {
            return ServiceResult.NotFound("User not found.");
        }

        var roleName = user.Role?.Name?.ToLowerInvariant() ?? string.Empty;
        // Normalize role name: handle both "MasterAdmin" -> "masteradmin" and "super_admin" -> "superadmin"
        var normalizedRoleName = roleName.Replace("_", "").Replace("-", "");
        // Check for admin roles using constants
        var isAdmin = normalizedRoleName == RoleNames.Normalized.Admin 
            || normalizedRoleName == RoleNames.Normalized.MasterAdmin 
            || normalizedRoleName == RoleNames.Normalized.DefaultAdmin 
            || normalizedRoleName == RoleNames.Normalized.SuperAdmin;

        if (!isAdmin)
        {
            return ServiceResult.Forbidden("Access denied. Admin role required.");
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

        var statusIds = await GetStatusIdsByCodesAsync(new[] { 
            BookingStatusCodes.Pending, 
            BookingStatusCodes.Assigned, 
            BookingStatusCodes.InProgress, 
            BookingStatusCodes.OnHold, 
            BookingStatusCodes.Completed 
        }, cancellationToken);
        var pendingStatusId = statusIds.GetValueOrDefault(BookingStatusCodes.Pending);
        var assignedStatusId = statusIds.GetValueOrDefault(BookingStatusCodes.Assigned);
        var inProgressStatusId = statusIds.GetValueOrDefault(BookingStatusCodes.InProgress);
        var holdStatusId = statusIds.GetValueOrDefault(BookingStatusCodes.OnHold);
        var completedStatusId = statusIds.GetValueOrDefault(BookingStatusCodes.Completed);

        var totalRequests = await bookingsQuery.CountAsync(cancellationToken);
        var pendingRequests = await bookingsQuery.CountAsync(b => b.StatusId == pendingStatusId, cancellationToken);
        var assignedRequests = await bookingsQuery.CountAsync(b => b.StatusId == assignedStatusId, cancellationToken);
        var inProgressRequests = await bookingsQuery.CountAsync(b => b.StatusId == inProgressStatusId, cancellationToken);
        var holdRequests = await bookingsQuery.CountAsync(b => b.StatusId == holdStatusId, cancellationToken);
        var completedRequests = await bookingsQuery.CountAsync(b => b.StatusId == completedStatusId, cancellationToken);

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
                .CountAsync(b => b.Pincode == pincode && b.StatusId == pendingStatusId, cancellationToken);

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

        var verifiedStatuses = new[] { VerificationStatusCodes.Verified, VerificationStatusCodes.Approved };
        var verificationStatusCode = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == providerId.Value)
            .Join(_context.VerificationStatuses.AsNoTracking(),
                user => user.VerificationStatusId,
                status => status.Id,
                (user, status) => status.Code)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(verificationStatusCode) ||
            !verifiedStatuses.Contains(verificationStatusCode.Trim().ToLowerInvariant()))
        {
            return ServiceResult.Forbidden("Service provider is not verified.");
        }

        var statusIds = await GetStatusIdsByCodesAsync(new[] { 
            BookingStatusCodes.Assigned, 
            BookingStatusCodes.InProgress, 
            BookingStatusCodes.Completed 
        }, cancellationToken);
        var assignedStatusId = statusIds.GetValueOrDefault(BookingStatusCodes.Assigned);
        var inProgressStatusId = statusIds.GetValueOrDefault(BookingStatusCodes.InProgress);
        var completedStatusId = statusIds.GetValueOrDefault(BookingStatusCodes.Completed);
        var activeStatusIds = new[] { assignedStatusId, inProgressStatusId }.Where(id => id > 0).ToList();

        var assignedBookings = await _context.BookingRequests
            .AsNoTracking()
            .Where(b => b.ServiceProviderId == providerId.Value && activeStatusIds.Contains(b.StatusId))
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        var completedBookings = await _context.BookingRequests
            .AsNoTracking()
            .Where(b => b.ServiceProviderId == providerId.Value && b.StatusId == completedStatusId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        var totalAssignments = await _context.BookingRequests
            .AsNoTracking()
            .CountAsync(b => b.ServiceProviderId == providerId.Value, cancellationToken);

        var pendingAssignments = await _context.BookingRequests
            .AsNoTracking()
            .CountAsync(b => b.ServiceProviderId == providerId.Value && activeStatusIds.Contains(b.StatusId), cancellationToken);

        var completedAssignments = await _context.BookingRequests
            .AsNoTracking()
            .CountAsync(b => b.ServiceProviderId == providerId.Value && b.StatusId == completedStatusId, cancellationToken);

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

        // Check if user is a service provider
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        var isServiceProvider = user != null && user.RoleId == (int)RoleEnum.ServiceProvider;

        if (isServiceProvider)
        {
            // Use service provider preferences table
            var preferences = await _context.ServiceProviderPincodePreferences
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
        else
        {
            // Use customer preferences table
            var preferences = await _context.CustomerPincodePreferences
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

        // Check if user is a service provider
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        var isServiceProvider = user != null && user.RoleId == (int)RoleEnum.ServiceProvider;

        if (isServiceProvider)
        {
            // Use service provider preferences table
            if (request.IsPrimary)
            {
                var existingPrimaries = await _context.ServiceProviderPincodePreferences
                    .Where(pref => pref.UserId == userId.Value && pref.IsPrimary)
                    .ToListAsync(cancellationToken);

                foreach (var preference in existingPrimaries)
                {
                    preference.IsPrimary = false;
                    preference.UpdatedAt = DateTime.UtcNow;
                }
            }

            var existingPreference = await _context.ServiceProviderPincodePreferences
                .FirstOrDefaultAsync(pref => pref.UserId == userId.Value && pref.Pincode == normalizedPincode, cancellationToken);

            if (existingPreference != null)
            {
                existingPreference.IsPrimary = request.IsPrimary;
                existingPreference.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.ServiceProviderPincodePreferences.Add(new ServiceProviderPincodePreference
                {
                    UserId = userId.Value,
                    Pincode = normalizedPincode,
                    IsPrimary = request.IsPrimary
                });
            }
        }
        else
        {
            // Use customer preferences table
            if (request.IsPrimary)
            {
                var existingPrimaries = await _context.CustomerPincodePreferences
                    .Where(pref => pref.UserId == userId.Value && pref.IsPrimary)
                    .ToListAsync(cancellationToken);

                foreach (var preference in existingPrimaries)
                {
                    preference.IsPrimary = false;
                    preference.UpdatedAt = DateTime.UtcNow;
                }
            }

            var existingPreference = await _context.CustomerPincodePreferences
                .FirstOrDefaultAsync(pref => pref.UserId == userId.Value && pref.Pincode == normalizedPincode, cancellationToken);

            if (existingPreference != null)
            {
                existingPreference.IsPrimary = request.IsPrimary;
                existingPreference.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.CustomerPincodePreferences.Add(new CustomerPincodePreference
                {
                    UserId = userId.Value,
                    Pincode = normalizedPincode,
                    IsPrimary = request.IsPrimary
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok(new OperationResponse { Success = true });
    }

    public async Task<ServiceResult> DeleteUserPincodePreferenceAsync(Guid? userId, Guid preferenceId, CancellationToken cancellationToken = default)
    {
        if (!userId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        // Check if user is a service provider
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        var isServiceProvider = user != null && user.RoleId == (int)RoleEnum.ServiceProvider;

        if (isServiceProvider)
        {
            // Use service provider preferences table
            var preference = await _context.ServiceProviderPincodePreferences
                .FirstOrDefaultAsync(pref => pref.Id == preferenceId && pref.UserId == userId.Value, cancellationToken);

            if (preference == null)
            {
                return ServiceResult.NotFound("Pincode preference not found.");
            }

            // If deleting primary, set the first available as primary
            if (preference.IsPrimary)
            {
                var otherPreferences = await _context.ServiceProviderPincodePreferences
                    .Where(pref => pref.UserId == userId.Value && pref.Id != preferenceId)
                    .OrderByDescending(pref => pref.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                if (otherPreferences != null)
                {
                    otherPreferences.IsPrimary = true;
                    otherPreferences.UpdatedAt = DateTime.UtcNow;
                }
            }

            _context.ServiceProviderPincodePreferences.Remove(preference);
        }
        else
        {
            // Use customer preferences table
            var preference = await _context.CustomerPincodePreferences
                .FirstOrDefaultAsync(pref => pref.Id == preferenceId && pref.UserId == userId.Value, cancellationToken);

            if (preference == null)
            {
                return ServiceResult.NotFound("Pincode preference not found.");
            }

            // If deleting primary, set the first available as primary
            if (preference.IsPrimary)
            {
                var otherPreferences = await _context.CustomerPincodePreferences
                    .Where(pref => pref.UserId == userId.Value && pref.Id != preferenceId)
                    .OrderByDescending(pref => pref.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                if (otherPreferences != null)
                {
                    otherPreferences.IsPrimary = true;
                    otherPreferences.UpdatedAt = DateTime.UtcNow;
                }
            }

            _context.CustomerPincodePreferences.Remove(preference);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok(new OperationResponse { Success = true, Message = "Pincode preference deleted successfully." });
    }

    public async Task<ServiceResult> GetServiceTypesAsync(int? serviceId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ServiceTypes
            .AsNoTracking()
            .Where(st => st.IsActive);

        // Filter by serviceId if provided
        if (serviceId.HasValue)
        {
            query = query.Where(st => st.ServiceId == serviceId.Value || st.ServiceId == null);
        }

        var serviceTypes = await query
            .OrderBy(st => st.Name)
            .ToListAsync(cancellationToken);

        var response = new ServiceTypesListResponse
        {
            Success = true,
            ServiceTypes = serviceTypes.Select(st => new ServiceTypeResponse
            {
                Id = st.Id,
                Name = st.Name,
                Description = st.Description,
                IsActive = st.IsActive
            }).ToList()
        };

        return ServiceResult.Ok(response);
    }

    public async Task<ServiceResult> GetAvailableTimeSlotsAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        // Define standard 3-hour time slots
        var allSlots = new[]
        {
            new { Slot = "9-12", DisplayName = "9:00 AM - 12:00 PM", StartHour = 9 },
            new { Slot = "12-3", DisplayName = "12:00 PM - 3:00 PM", StartHour = 12 },
            new { Slot = "3-6", DisplayName = "3:00 PM - 6:00 PM", StartHour = 15 }
        };

        var dateOnly = date.Date;

        var timeSlots = allSlots.Select(slot => new TimeSlotItem
        {
            Slot = slot.Slot,
            DisplayName = slot.DisplayName,
            IsAvailable = true
        }).ToList();

        var response = new TimeSlotsResponse
        {
            Success = true,
            Date = dateOnly,
            TimeSlots = timeSlots
        };

        return await Task.FromResult(ServiceResult.Ok(response));
    }

    public async Task<ServiceResult> GetBookingSummaryAsync(
        Guid? userId,
        int serviceId,
        int? serviceTypeId,
        string pincode,
        DateTime? preferredDate,
        string? timeSlot,
        CancellationToken cancellationToken = default)
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
            return ServiceResult.BadRequest("Pincode not found in our service areas.");
        }

        var service = await _context.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.IsActive, cancellationToken);

        if (service == null)
        {
            return ServiceResult.BadRequest("Service not found.");
        }

        var serviceType = serviceTypeId.HasValue
            ? await _context.ServiceTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(st => st.Id == serviceTypeId.Value && st.IsActive, cancellationToken)
            : null;

        var basePrice = await _context.ServicePrices
            .AsNoTracking()
            .Where(price => price.IsActive && price.ServiceId == serviceId)
            .OrderByDescending(price => price.EffectiveFrom)
            .Select(price => (decimal?)price.Charges)
            .FirstOrDefaultAsync(cancellationToken);

        var priceRating = 1; // Default price rating
        var calculatedPrice = basePrice.HasValue ? basePrice.Value * priceRating : 0;

        var response = new BookingSummaryResponse
        {
            Success = true,
            ServiceId = serviceId,
            ServiceName = service.ServiceName,
            ServiceTypeId = serviceTypeId,
            ServiceTypeName = serviceType?.Name,
            Pincode = normalizedPincode,
            PreferredDate = preferredDate,
            TimeSlot = timeSlot,
            BasePrice = basePrice ?? 0,
            CalculatedPrice = calculatedPrice
        };

        return ServiceResult.Ok(response);
    }

    public async Task<ServiceResult> CancelBookingAsync(Guid? userId, Guid bookingId, CancellationToken cancellationToken = default)
    {
        if (!userId.HasValue)
        {
            return ServiceResult.Unauthorized();
        }

        var booking = await _context.BookingRequests
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        if (booking == null)
        {
            return ServiceResult.Ok(new BookingResponse
            {
                Success = false,
                Message = "Booking not found"
            });
        }

        // Verify that the booking belongs to the customer
        if (booking.CustomerId != userId.Value)
        {
            return ServiceResult.Forbidden("You can only cancel your own bookings.");
        }

        // Get status IDs for validation
        var statusIds = await GetStatusIdsByCodesAsync(new[] { 
            BookingStatusCodes.Cancelled, 
            BookingStatusCodes.Completed 
        }, cancellationToken);
        var cancelledStatusId = statusIds.GetValueOrDefault(BookingStatusCodes.Cancelled);
        var completedStatusId = statusIds.GetValueOrDefault(BookingStatusCodes.Completed);

        // Check if booking is already cancelled
        if (booking.StatusId == cancelledStatusId)
        {
            return ServiceResult.Ok(new BookingResponse
            {
                Success = false,
                Message = "Booking is already cancelled"
            });
        }

        // Check if booking is already completed
        if (booking.StatusId == completedStatusId)
        {
            return ServiceResult.Ok(new BookingResponse
            {
                Success = false,
                Message = "Cannot cancel a completed booking"
            });
        }

        // Update booking status to cancelled
        booking.StatusId = cancelledStatusId;
        booking.Status = BookingStatusStrings.Cancelled; // Backward compatibility
        booking.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok(new BookingResponse
        {
            Success = true,
            Booking = await BuildBookingDtoAsync(booking, cancellationToken),
            Message = "Booking cancelled successfully"
        });
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

        var serviceIdsInPincode = await _context.ServiceProviderPincodePreferences
            .AsNoTracking()
            .Where(pref => pref.Pincode == pincode)
            .Join(_context.ProviderServices.AsNoTracking().Where(ps => ps.IsActive),
                pref => pref.UserId,
                ps => ps.UserId,
                (pref, ps) => ps.ServiceId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (serviceIdsInPincode.Count == 0)
        {
            return response;
        }

        var services = await _context.Services
            .AsNoTracking()
            .Include(s => s.Category)
            .Where(s => s.IsActive && serviceIdsInPincode.Contains(s.Id))
            .ToListAsync(cancellationToken);

        response.Success = true;
        response.Pincode = pincode;
        response.Services = services.Select(service => new ServiceAvailabilityItem
        {
            ServiceId = service.Id.ToString(),
            ServiceName = service.ServiceName,
            Description = service.Description,
            CategoryId = service.CategoryId ?? 0,
            Image = service.Image,
            Icon = service.Icon,
            CategoryImage = service.Category?.Image,
            CategoryIcon = service.Category?.Icon,
            CategoryName = service.Category?.CategoryName ?? "Uncategorized",
            IsAvailable = true,
            BasePrice = 0,
            CalculatedPrice = 0,
            PriceRating = 1
        }).ToList();

        return response;
    }

    private BookingRequestDto BuildBookingDto(
        BookingRequest booking,
        Dictionary<int, Service> services,
        Dictionary<Guid, UsersExtraInfo> userExtraDict,
        Dictionary<int, ServiceType> serviceTypes)
    {
        services.TryGetValue(booking.ServiceId, out var service);
        
        BookingUserDto? MapUser(Guid userId)
        {
            if (!userExtraDict.TryGetValue(userId, out var info))
            {
                return new BookingUserDto { Id = userId };
            }
            
            return new BookingUserDto
            {
                Id = userId,
                Name = info.FullName,
                Email = info.Email,
                Phone = info.PhoneNumber
            };
        }

        BookingAdminDto? MapAdmin(Guid adminId)
        {
            if (!userExtraDict.TryGetValue(adminId, out var info))
            {
                return new BookingAdminDto { Id = adminId };
            }
            
            return new BookingAdminDto
            {
                Id = adminId,
                Name = info.FullName,
                Email = info.Email
            };
        }

        ServiceType? serviceType = null;
        if (booking.ServiceTypeId.HasValue)
        {
            serviceTypes.TryGetValue(booking.ServiceTypeId.Value, out serviceType);
        }

        UsersExtraInfo? serviceProviderInfo = null;
        if (booking.ServiceProviderId.HasValue)
        {
            userExtraDict.TryGetValue(booking.ServiceProviderId.Value, out serviceProviderInfo);
        }

        return new BookingRequestDto
        {
            Id = booking.Id,
            CustomerId = booking.CustomerId,
            ServiceId = booking.ServiceId,
            ServiceTypeId = booking.ServiceTypeId,
            ServiceTypeName = serviceType?.Name,
            Pincode = booking.Pincode,
            ServiceProviderId = booking.ServiceProviderId,
            AdminId = booking.AdminId,
            Status = booking.Status,
            RequestDescription = booking.RequestDescription,
            CustomerAddress = booking.CustomerAddress,
            AddressLine1 = booking.AddressLine1,
            AddressLine2 = booking.AddressLine2,
            City = booking.City,
            State = booking.State,
            CustomerPhone = booking.CustomerPhone,
            AlternativeMobileNumber = booking.AlternativeMobileNumber,
            CustomerName = booking.CustomerName,
            PreferredDate = booking.PreferredDate,
            PreferredTime = booking.PreferredTime,
            TimeSlot = booking.TimeSlot,
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
                    Name = serviceProviderInfo?.FullName,
                    Email = serviceProviderInfo?.Email,
                    Phone = serviceProviderInfo?.PhoneNumber
                }
                : null,
            Admin = booking.AdminId.HasValue ? MapAdmin(booking.AdminId.Value) : null
        };
    }

    private async Task<BookingRequestDto> BuildBookingDtoAsync(BookingRequest booking, CancellationToken cancellationToken)
    {
        // Ensure StatusNavigation is loaded if not already
        if (booking.StatusNavigation == null && booking.StatusId > 0)
        {
            await _context.Entry(booking)
                .Reference(b => b.StatusNavigation)
                .LoadAsync(cancellationToken);
        }

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

        var serviceType = booking.ServiceTypeId.HasValue
            ? await _context.ServiceTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(st => st.Id == booking.ServiceTypeId.Value, cancellationToken)
            : null;

        return new BookingRequestDto
        {
            Id = booking.Id,
            CustomerId = booking.CustomerId,
            ServiceId = booking.ServiceId,
            ServiceTypeId = booking.ServiceTypeId,
            ServiceTypeName = serviceType?.Name,
            Pincode = booking.Pincode,
            ServiceProviderId = booking.ServiceProviderId,
            AdminId = booking.AdminId,
            Status = booking.Status,
            RequestDescription = booking.RequestDescription,
            CustomerAddress = booking.CustomerAddress,
            AddressLine1 = booking.AddressLine1,
            AddressLine2 = booking.AddressLine2,
            City = booking.City,
            State = booking.State,
            CustomerPhone = booking.CustomerPhone,
            AlternativeMobileNumber = booking.AlternativeMobileNumber,
            CustomerName = booking.CustomerName,
            PreferredDate = booking.PreferredDate,
            PreferredTime = booking.PreferredTime,
            TimeSlot = booking.TimeSlot,
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

    public async Task<ServiceResult> GetAvailableServiceProvidersAsync(int serviceId, string pincode, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pincode))
            {
                return ServiceResult.BadRequest("Pincode is required.");
            }

            var normalizedPincode = pincode.Trim();
            var approvedStatusId = (int)VerificationStatusEnum.Approved;

            // Get service providers who:
            // 1. Offer this service (ProviderService)
            // 2. Serve this pincode (ServiceProviderPincodePreference)
            // 3. Are verified/approved (User.VerificationStatusId == Approved)
            // 4. Are active

            var serviceProviderRoleId = (int)RoleEnum.ServiceProvider;

            var availableProviders = await (
                from ps in _context.ProviderServices
                join pref in _context.ServiceProviderPincodePreferences
                    on ps.UserId equals pref.UserId
                join user in _context.Users
                    on ps.UserId equals user.Id
                join userInfo in _context.UsersExtraInfos
                    on ps.UserId equals userInfo.UserId
                where ps.ServiceId == serviceId
                    && ps.IsActive
                    && pref.Pincode == normalizedPincode
                    && user.VerificationStatusId == approvedStatusId
                    && user.RoleId == serviceProviderRoleId
                select new AvailableServiceProvider
                {
                    Id = ps.UserId,
                    Name = userInfo.FullName,
                    Email = userInfo.Email,
                    Phone = string.IsNullOrWhiteSpace(userInfo.PhoneNumber) ? null : userInfo.PhoneNumber
                }
            ).Distinct().ToListAsync(cancellationToken);

            // If no providers found with verification, try to get providers without verification check
            // (fallback for providers who might not have verification record yet)
            if (availableProviders.Count == 0)
            {
                availableProviders = await (
                    from ps in _context.ProviderServices
                    join pref in _context.ServiceProviderPincodePreferences
                        on ps.UserId equals pref.UserId
                    join user in _context.Users
                        on ps.UserId equals user.Id
                    join userInfo in _context.UsersExtraInfos
                        on ps.UserId equals userInfo.UserId
                    where ps.ServiceId == serviceId
                        && ps.IsActive
                        && pref.Pincode == normalizedPincode
                        && user.RoleId == serviceProviderRoleId
                    select new AvailableServiceProvider
                    {
                        Id = ps.UserId,
                        Name = userInfo.FullName,
                        Email = userInfo.Email,
                        Phone = string.IsNullOrWhiteSpace(userInfo.PhoneNumber) ? null : userInfo.PhoneNumber
                    }
                ).Distinct().ToListAsync(cancellationToken);
            }

            var response = new AvailableServiceProvidersResponse
            {
                Success = true,
                ServiceProviders = availableProviders,
                Message = availableProviders.Count > 0 
                    ? $"Found {availableProviders.Count} available service provider(s)" 
                    : "No service providers available for this service in the specified pincode"
            };

            return ServiceResult.Ok(response);
        }
        catch (Exception)
        {
            return new ServiceResult
            {
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
                Payload = new AvailableServiceProvidersResponse
                {
                    Success = false,
                    Message = "An error occurred while retrieving available service providers",
                    ServiceProviders = new List<AvailableServiceProvider>()
                }
            };
        }
    }
}
