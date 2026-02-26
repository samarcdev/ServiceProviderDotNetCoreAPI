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

    /// <summary>
    /// Filters services based on service provider leave status when customer is logged in.
    /// Logic: 
    /// - If service has only one provider and that provider is on leave, exclude the service
    /// - If service has multiple providers, include it only if at least one provider is not on leave
    /// </summary>
    private async Task<List<int>> FilterServicesByProviderLeaveStatusAsync(List<int> serviceIds, CancellationToken cancellationToken = default)
    {
        if (serviceIds == null || serviceIds.Count == 0)
        {
            return new List<int>();
        }

        var checkDate = DateTime.UtcNow.Date;

        // Get all service providers for these services
        var serviceProviderMapping = await _context.ProviderServices
            .AsNoTracking()
            .Where(ps => ps.IsActive && serviceIds.Contains(ps.ServiceId))
            .GroupBy(ps => ps.ServiceId)
            .Select(g => new
            {
                ServiceId = g.Key,
                ProviderIds = g.Select(ps => ps.UserId).Distinct().ToList()
            })
            .ToListAsync(cancellationToken);

        // Get all providers who are on leave today
        var allProviderIds = serviceProviderMapping.SelectMany(m => m.ProviderIds).Distinct().ToList();
        var providersOnLeave = await _context.ServiceProviderLeaveDays
            .AsNoTracking()
            .Where(l => allProviderIds.Contains(l.ServiceProviderId) && l.LeaveDate.Date == checkDate)
            .Select(l => l.ServiceProviderId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var providersOnLeaveSet = providersOnLeave.ToHashSet();

        // Filter services based on provider availability
        var availableServiceIds = new List<int>();
        foreach (var mapping in serviceProviderMapping)
        {
            var providerIds = mapping.ProviderIds;
            var availableProviders = providerIds.Where(p => !providersOnLeaveSet.Contains(p)).ToList();

            // If service has only one provider and that provider is on leave, exclude the service
            if (providerIds.Count == 1 && providersOnLeaveSet.Contains(providerIds[0]))
            {
                continue;
            }

            // If service has multiple providers, include it only if at least one provider is not on leave
            if (providerIds.Count > 1 && availableProviders.Count == 0)
            {
                continue;
            }

            // Service is available (either has multiple providers with at least one available, or single provider not on leave)
            availableServiceIds.Add(mapping.ServiceId);
        }

        return availableServiceIds;
    }

    public async Task<ServiceResult> GetAllAvailableServicesAsync(Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        var services = await _context.Services
            .AsNoTracking()
            .Where(service => service.IsActive)
            .Include(service => service.Category)
            .ToListAsync(cancellationToken);

        // If customer is logged in, filter services based on service provider leave status
        if (customerId.HasValue)
        {
            var serviceIdsToFilter = services.Select(s => s.Id).ToList();
            var availableServiceIds = await FilterServicesByProviderLeaveStatusAsync(serviceIdsToFilter, cancellationToken);
            services = services.Where(s => availableServiceIds.Contains(s.Id)).ToList();
        }

        // Get prices for all services
        var serviceIds = services.Select(service => service.Id).ToList();
        var allServicePrices = await _context.ServicePrices
            .AsNoTracking()
            .Where(price => price.IsActive && price.ServiceId.HasValue && serviceIds.Contains(price.ServiceId.Value))
            .ToListAsync(cancellationToken);

        var servicePrices = allServicePrices
            .GroupBy(price => price.ServiceId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(price => price.EffectiveFrom).First().Charges
            );

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
                CalculatedPrice = servicePrices.TryGetValue(service.Id, out var price) ? price : 0,
                PriceRating = 1
            }).ToList()
        };

        return ServiceResult.Ok(response);
    }

    public async Task<ServiceResult> GetTopBookedServicesAsync(int days = 90, int limit = 24, Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        var lookbackDays = days <= 0 ? 90 : Math.Min(days, 365);
        var takeLimit = limit <= 0 ? 24 : Math.Min(limit, 100);
        var sinceDate = DateTime.UtcNow.AddDays(-lookbackDays);

        var cancelledStatusId = await _context.BookingStatuses
            .AsNoTracking()
            .Where(s => s.IsActive && s.Code == BookingStatusCodes.Cancelled)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var topServiceCounts = await _context.BookingRequests
            .AsNoTracking()
            .Where(booking =>
                booking.CreatedAt >= sinceDate &&
                (!cancelledStatusId.HasValue || booking.StatusId != cancelledStatusId.Value))
            .GroupBy(booking => booking.ServiceId)
            .Select(group => new
            {
                ServiceId = group.Key,
                BookingCount = group.Count()
            })
            .OrderByDescending(item => item.BookingCount)
            .Take(takeLimit)
            .ToListAsync(cancellationToken);

        var topServiceIds = topServiceCounts
            .Select(item => item.ServiceId)
            .ToList();

        if (topServiceIds.Count == 0)
        {
            topServiceIds = await _context.Services
                .AsNoTracking()
                .Where(service => service.IsActive)
                .OrderBy(service => service.ServiceName)
                .Select(service => service.Id)
                .Take(takeLimit)
                .ToListAsync(cancellationToken);
        }

        var services = await _context.Services
            .AsNoTracking()
            .Include(service => service.Category)
            .Where(service => service.IsActive && topServiceIds.Contains(service.Id))
            .ToListAsync(cancellationToken);

        var serviceIdOrder = topServiceIds
            .Select((serviceId, index) => new { serviceId, index })
            .ToDictionary(item => item.serviceId, item => item.index);

        var orderedServices = services
            .OrderBy(service => serviceIdOrder.TryGetValue(service.Id, out var index) ? index : int.MaxValue)
            .ToList();

        // If customer is logged in, filter services based on service provider leave status
        if (customerId.HasValue)
        {
            var serviceIdsToFilter = orderedServices.Select(s => s.Id).ToList();
            var availableServiceIds = await FilterServicesByProviderLeaveStatusAsync(serviceIdsToFilter, cancellationToken);
            orderedServices = orderedServices.Where(s => availableServiceIds.Contains(s.Id)).ToList();
        }

        var serviceIds = orderedServices.Select(service => service.Id).ToList();
        var allServicePrices = await _context.ServicePrices
            .AsNoTracking()
            .Where(price => price.IsActive && price.ServiceId.HasValue && serviceIds.Contains(price.ServiceId.Value))
            .ToListAsync(cancellationToken);

        var servicePrices = allServicePrices
            .GroupBy(price => price.ServiceId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(price => price.EffectiveFrom).First().Charges
            );

        var response = new ServiceAvailabilityResponse
        {
            Success = true,
            Message = "Top booked services",
            Services = orderedServices.Select(service => new ServiceAvailabilityItem
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
                CalculatedPrice = servicePrices.TryGetValue(service.Id, out var price) ? price : 0,
                PriceRating = 1
            }).ToList()
        };

        return ServiceResult.Ok(response);
    }

    public async Task<ServiceResult> GetAvailableServicesByPincodeAsync(string pincode, Guid? customerId = null, CancellationToken cancellationToken = default)
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

        // Optimized query: First get user IDs for the pincode, then get their active services
        var userIdsInPincode = await _context.ServiceProviderPincodePreferences
            .AsNoTracking()
            .Where(pref => pref.Pincode == normalizedPincode)
            .Select(pref => pref.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (userIdsInPincode.Count == 0)
        {
            return ServiceResult.Ok(new ServiceAvailabilityResponse
            {
                Success = false,
                Message = "No services are currently available in your area. Please try a different pincode.",
                Services = new List<ServiceAvailabilityItem>()
            });
        }

        var serviceIdsInPincode = await _context.ProviderServices
            .AsNoTracking()
            .Where(ps => ps.IsActive && userIdsInPincode.Contains(ps.UserId))
            .Select(ps => ps.ServiceId)
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

        // If customer is logged in, filter services based on service provider leave status
        // Note: We need to consider only providers who serve this pincode
        if (customerId.HasValue)
        {
            var serviceIdsToFilter = services.Select(s => s.Id).ToList();
            var checkDate = DateTime.UtcNow.Date;

            // Get service providers for these services who serve this pincode
            var serviceProviderMapping = await (
                from ps in _context.ProviderServices
                join pref in _context.ServiceProviderPincodePreferences
                    on ps.UserId equals pref.UserId
                where ps.IsActive && serviceIdsToFilter.Contains(ps.ServiceId) && pref.Pincode == normalizedPincode
                group ps by ps.ServiceId into g
                select new
                {
                    ServiceId = g.Key,
                    ProviderIds = g.Select(ps => ps.UserId).Distinct().ToList()
                }
            ).ToListAsync(cancellationToken);

            // Get all providers who are on leave today
            var allProviderIds = serviceProviderMapping.SelectMany(m => m.ProviderIds).Distinct().ToList();
            var providersOnLeave = await _context.ServiceProviderLeaveDays
                .AsNoTracking()
                .Where(l => allProviderIds.Contains(l.ServiceProviderId) && l.LeaveDate.Date == checkDate)
                .Select(l => l.ServiceProviderId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var providersOnLeaveSet = providersOnLeave.ToHashSet();

            // Filter services based on provider availability
            var availableServiceIds = new HashSet<int>();
            foreach (var mapping in serviceProviderMapping)
            {
                var providerIds = mapping.ProviderIds;
                var availableProviders = providerIds.Where(p => !providersOnLeaveSet.Contains(p)).ToList();

                // If service has only one provider and that provider is on leave, exclude the service
                if (providerIds.Count == 1 && providersOnLeaveSet.Contains(providerIds[0]))
                {
                    continue;
                }

                // If service has multiple providers, include it only if at least one provider is not on leave
                if (providerIds.Count > 1 && availableProviders.Count == 0)
                {
                    continue;
                }

                // Service is available (either has multiple providers with at least one available, or single provider not on leave)
                availableServiceIds.Add(mapping.ServiceId);
            }

            services = services.Where(s => availableServiceIds.Contains(s.Id)).ToList();
        }

        // Get prices for all services
        var serviceIds = services.Select(service => service.Id).ToList();
        var allServicePrices = await _context.ServicePrices
            .AsNoTracking()
            .Where(price => price.IsActive && price.ServiceId.HasValue && serviceIds.Contains(price.ServiceId.Value))
            .ToListAsync(cancellationToken);

        var servicePrices = allServicePrices
            .GroupBy(price => price.ServiceId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(price => price.EffectiveFrom).First().Charges
            );

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
                CalculatedPrice = servicePrices.TryGetValue(service.Id, out var price) ? price : 0,
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
                PriceRating = priceRating,
                CalculatedPrice = 0
            });
        }

        var calculatedPrice = basePrice.Value * priceRating;

        return ServiceResult.Ok(new ServicePriceCalculationResponse
        {
            ServiceId = serviceId,
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
            .Include(cp => cp.City)
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

        // Validate discount if provided
        if (request.DiscountId.HasValue)
        {
            var discount = await _context.DiscountMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(d => 
                    d.DiscountId == request.DiscountId.Value 
                    && d.IsActive
                    && d.ValidFrom <= DateTime.UtcNow
                    && d.ValidTo >= DateTime.UtcNow,
                    cancellationToken);

            if (discount == null)
            {
                return ServiceResult.BadRequest(new BookingResponse
                {
                    Success = false,
                    Message = "Invalid or expired discount code."
                });
            }

            // Validate minimum order value if calculated price is provided
            if (request.CalculatedPrice.HasValue && request.CalculatedPrice.Value < discount.MinOrderValue)
            {
                return ServiceResult.BadRequest(new BookingResponse
                {
                    Success = false,
                    Message = $"Discount requires a minimum order value of {discount.MinOrderValue}."
                });
            }
        }

        // Use prices from request (calculated in summary), or calculate if not provided
        decimal? basePriceValue = request.BasePrice;
        decimal? locationAdjustmentAmount = request.LocationAdjustmentAmount;
        decimal? calculatedPrice = request.CalculatedPrice;
        decimal? priceAfterDiscount = request.PriceAfterDiscount;
        decimal? cgstAmount = request.CgstAmount;
        decimal? sgstAmount = request.SgstAmount;
        decimal? igstAmount = request.IgstAmount;
        decimal? totalTaxAmount = request.TotalTaxAmount;
        decimal? serviceChargeAmount = request.ServiceChargeAmount;
        decimal? platformChargeAmount = request.PlatformChargeAmount;
        decimal? finalPrice = request.FinalPrice;

        // If prices are not provided in request, calculate them (backward compatibility)
        if (!basePriceValue.HasValue || !calculatedPrice.HasValue)
        {
            var basePrice = await _context.ServicePrices
                .AsNoTracking()
                .Where(price => price.IsActive && price.ServiceId == request.ServiceId)
                .OrderByDescending(price => price.EffectiveFrom)
                .Select(price => (decimal?)price.Charges)
                .FirstOrDefaultAsync(cancellationToken);

            basePriceValue = basePrice.HasValue ? basePrice.Value : 0m;
            
            // Get location price adjustment
            var locationPriceAdjustment = await _context.LocationPriceAdjustments
                .AsNoTracking()
                .FirstOrDefaultAsync(lpa => lpa.IsActive && lpa.CityPincodeId == pincodeEntry.Id, cancellationToken);

            calculatedPrice = basePriceValue.Value;
            locationAdjustmentAmount = 0m;
            
            if (locationPriceAdjustment != null)
            {
                if (locationPriceAdjustment.PriceMultiplier.HasValue)
                {
                    calculatedPrice = basePriceValue.Value * locationPriceAdjustment.PriceMultiplier.Value;
                    locationAdjustmentAmount = calculatedPrice.Value - basePriceValue.Value;
                }
                else if (locationPriceAdjustment.FixedAdjustment.HasValue)
                {
                    locationAdjustmentAmount = locationPriceAdjustment.FixedAdjustment.Value;
                    calculatedPrice = basePriceValue.Value + locationAdjustmentAmount.Value;
                }
            }

            // Apply discount if provided
            if (request.DiscountId.HasValue && calculatedPrice.HasValue)
            {
                var discount = await _context.DiscountMasters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.DiscountId == request.DiscountId.Value, cancellationToken);

                if (discount != null && discount.DiscountValue.HasValue)
                {
                    decimal discountAmount = 0m;
                    if (discount.DiscountType?.ToLowerInvariant() == "percentage")
                    {
                        discountAmount = calculatedPrice.Value * (discount.DiscountValue.Value / 100m);
                    }
                    else
                    {
                        discountAmount = discount.DiscountValue.Value;
                    }
                    
                    finalPrice = calculatedPrice.Value - discountAmount;
                    if (finalPrice < 0) finalPrice = 0;
                }
                else
                {
                    finalPrice = calculatedPrice;
                }
            }
            else
            {
                priceAfterDiscount = calculatedPrice;
                finalPrice = calculatedPrice;
            }
        }

        // Calculate taxes and service charges if not provided in request
        if (!cgstAmount.HasValue || !serviceChargeAmount.HasValue || !finalPrice.HasValue)
        {
            // Get company configuration for customer state comparison
            var companyConfig = await _context.CompanyConfigurations
                .AsNoTracking()
                .Include(cc => cc.CompanyState)
                .FirstOrDefaultAsync(cc => cc.IsActive, cancellationToken);
            
            // Get customer state from pincode
            var customerStateId = pincodeEntry.City?.StateId;
            var companyStateId = companyConfig?.CompanyStateId;
            bool isSameState = customerStateId.HasValue && companyStateId.HasValue && customerStateId.Value == companyStateId.Value;

            // Get all active tax rates from tax_master
            var cgstTax = await _context.TaxMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IsActive && t.TaxName.ToUpper() == "CGST", cancellationToken);
            var sgstTax = await _context.TaxMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IsActive && t.TaxName.ToUpper() == "SGST", cancellationToken);
            var igstTax = await _context.TaxMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IsActive && t.TaxName.ToUpper() == "IGST", cancellationToken);
            var serviceChargeTax = await _context.TaxMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IsActive && t.TaxName.ToUpper() == "SERVICE CHARGE", cancellationToken);
            var platformChargeTax = await _context.TaxMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IsActive && t.TaxName.ToUpper() == "PLATFORM CHARGE", cancellationToken);

            // Calculate taxes based on price after discount
            decimal calculatedCgstAmount = 0m;
            decimal calculatedSgstAmount = 0m;
            decimal calculatedIgstAmount = 0m;

            if (isSameState)
            {
                // Same state: CGST + SGST (9% + 9% = 18%)
                if (cgstTax != null && priceAfterDiscount.HasValue)
                {
                    calculatedCgstAmount = priceAfterDiscount.Value * (cgstTax.TaxPercentage / 100m);
                }
                if (sgstTax != null && priceAfterDiscount.HasValue)
                {
                    calculatedSgstAmount = priceAfterDiscount.Value * (sgstTax.TaxPercentage / 100m);
                }
            }
            else
            {
                // Different state: IGST (18%)
                if (igstTax != null && priceAfterDiscount.HasValue)
                {
                    calculatedIgstAmount = priceAfterDiscount.Value * (igstTax.TaxPercentage / 100m);
                }
            }

            cgstAmount = calculatedCgstAmount;
            sgstAmount = calculatedSgstAmount;
            igstAmount = calculatedIgstAmount;
            totalTaxAmount = calculatedCgstAmount + calculatedSgstAmount + calculatedIgstAmount;

            // Calculate service charge from tax_master (only if active)
            decimal calculatedServiceChargeAmount = 0m;
            if (serviceChargeTax != null && priceAfterDiscount.HasValue)
            {
                calculatedServiceChargeAmount = priceAfterDiscount.Value * (serviceChargeTax.TaxPercentage / 100m);
            }
            if (!serviceChargeAmount.HasValue)
            {
                serviceChargeAmount = calculatedServiceChargeAmount;
            }

            // Calculate platform charge from tax_master (only if active)
            decimal calculatedPlatformChargeAmount = 0m;
            if (platformChargeTax != null && priceAfterDiscount.HasValue)
            {
                calculatedPlatformChargeAmount = priceAfterDiscount.Value * (platformChargeTax.TaxPercentage / 100m);
            }
            if (!platformChargeAmount.HasValue)
            {
                platformChargeAmount = calculatedPlatformChargeAmount;
            }

            // Recalculate final price: price after discount + taxes + service charge + platform charge
            if (priceAfterDiscount.HasValue)
            {
                finalPrice = priceAfterDiscount.Value + totalTaxAmount.Value + serviceChargeAmount.Value + platformChargeAmount.Value;
            }
        }

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

        // Find admin assigned to the state based on pincode location
        Guid? assignedAdminId = null;
        
        // Get the state ID from the pincode's city
        if (pincodeEntry?.City?.StateId.HasValue == true)
        {
            // Find admin assigned to this state
            assignedAdminId = await _context.AdminStateAssignments
                .AsNoTracking()
                .Where(asa => asa.StateId == pincodeEntry.City.StateId.Value)
                .Select(asa => (Guid?)asa.AdminUserId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // If no admin found for the location, assign to default admin
        if (!assignedAdminId.HasValue)
        {
            assignedAdminId = await GetDefaultAdminIdAsync(cancellationToken);
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
            BasePrice = basePriceValue,
            LocationAdjustmentAmount = locationAdjustmentAmount,
            EstimatedPrice = calculatedPrice,
            DiscountId = request.DiscountId,
            DiscountAmount = request.DiscountAmount,
            CgstAmount = cgstAmount,
            SgstAmount = sgstAmount,
            IgstAmount = igstAmount,
            TotalTaxAmount = totalTaxAmount,
            ServiceChargeAmount = serviceChargeAmount,
            PlatformChargeAmount = platformChargeAmount,
            FinalPrice = finalPrice,
            WorkingHours = request.WorkingHours ?? 1,
            AdminId = assignedAdminId,
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
        string? search,
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
                        return ServiceResult.Ok(new PagedBookingRequestsResponse
                        {
                            Items = new List<BookingRequestDto>(),
                            TotalCount = 0,
                            Page = page,
                            PageSize = limit,
                            TotalPages = 0
                        });
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

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim().ToLower();
                var matchingServiceIds = await _context.Services
                    .AsNoTracking()
                    .Where(s => s.ServiceName.ToLower().Contains(searchTerm))
                    .Select(s => s.Id)
                    .ToListAsync(cancellationToken);
                query = query.Where(b =>
                    (b.CustomerName != null && b.CustomerName.ToLower().Contains(searchTerm)) ||
                    matchingServiceIds.Contains(b.ServiceId) ||
                    b.Id.ToString().ToLower().Contains(searchTerm));
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

            var totalCount = await orderedQuery.CountAsync(cancellationToken);

            var bookings = await orderedQuery
                .Skip(skip)
                .Take(limit)
                .ToListAsync(cancellationToken);

            if (bookings.Count == 0)
            {
                return ServiceResult.Ok(new PagedBookingRequestsResponse
                {
                    Items = new List<BookingRequestDto>(),
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = limit,
                    TotalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)limit) : 0
                });
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
                var servicesList = await _context.Services
                    .AsNoTracking()
                    .Include(s => s.Category)
                    .Where(s => serviceIds.Contains(s.Id))
                    .ToListAsync(cancellationToken);
                services = servicesList.ToDictionary(s => s.Id);
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
                    System.Diagnostics.Debug.WriteLine($"Error building DTO for booking {booking.Id}: {ex.Message}");
                }
            }

            return ServiceResult.Ok(new PagedBookingRequestsResponse
            {
                Items = result,
                TotalCount = totalCount,
                Page = page,
                PageSize = limit,
                TotalPages = (int)Math.Ceiling(totalCount / (double)limit)
            });
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

        var previousProviderId = booking.ServiceProviderId;
        var isReassignment = previousProviderId.HasValue && previousProviderId.Value != request.ServiceProviderId;

        booking.AdminId = adminId.Value;
        booking.AdminNotes = request.AdminNotes ?? booking.AdminNotes;
        booking.EstimatedPrice = request.EstimatedPrice ?? booking.EstimatedPrice;

        if (string.Equals(request.Status, VerificationStatusStrings.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            var rejectedStatusId = await GetStatusIdByCodeAsync(BookingStatusCodes.Rejected, cancellationToken);
            booking.StatusId = rejectedStatusId;
            booking.Status = BookingStatusStrings.Rejected; // Backward compatibility

            // If there was a previous assignment, mark it as unassigned with rejection reason
            if (previousProviderId.HasValue)
            {
                var previousAssignment = await _context.BookingAssignments
                    .FirstOrDefaultAsync(ba => ba.BookingRequestId == request.BookingId && ba.IsCurrent, cancellationToken);
                
                if (previousAssignment != null)
                {
                    previousAssignment.IsCurrent = false;
                    previousAssignment.UnassignedAt = DateTime.UtcNow;
                    previousAssignment.UnassignedReason = request.AdminNotes ?? "Booking rejected by admin";
                    previousAssignment.ReasonType = "rejection";
                }
            }
        }
        else
        {
            // If reassigning, mark previous assignment as not current
            if (isReassignment && previousProviderId.HasValue)
            {
                var previousAssignment = await _context.BookingAssignments
                    .FirstOrDefaultAsync(ba => ba.BookingRequestId == request.BookingId && ba.IsCurrent, cancellationToken);
                
                if (previousAssignment != null)
                {
                    previousAssignment.IsCurrent = false;
                    previousAssignment.UnassignedAt = DateTime.UtcNow;
                    previousAssignment.UnassignedReason = request.AdminNotes ?? "Reassigned to another service provider";
                    previousAssignment.ReasonType = "reassignment";
                }
            }

            booking.ServiceProviderId = request.ServiceProviderId;
            var statusCode = string.IsNullOrWhiteSpace(request.Status) ? BookingStatusCodes.Assigned : request.Status.ToUpperInvariant();
            var statusId = await GetStatusIdByCodeAsync(statusCode, cancellationToken);
            booking.StatusId = statusId;
            booking.Status = request.Status ?? BookingStatusStrings.Assigned; // Backward compatibility
            booking.AssignedAt = DateTime.UtcNow;

            // Create new booking assignment record
            if (request.ServiceProviderId.HasValue)
            {
                var assignment = new BookingAssignment
                {
                    BookingRequestId = request.BookingId,
                    ServiceProviderId = request.ServiceProviderId.Value,
                    AssignedByUserId = adminId.Value,
                    AssignedAt = DateTime.UtcNow,
                    IsCurrent = true,
                    Notes = request.AdminNotes,
                    ReasonType = isReassignment ? "reassignment" : "initial_assignment",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.BookingAssignments.Add(assignment);
            }
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

        string? statusCode = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            statusCode = request.Status.Trim().ToUpperInvariant();
            var statusId = await GetStatusIdByCodeAsync(statusCode, cancellationToken);
            booking.StatusId = statusId;
            booking.Status = request.Status; // Backward compatibility
        }

        booking.ServiceProviderNotes = request.ServiceProviderNotes ?? booking.ServiceProviderNotes;
        booking.CustomerRating = request.CustomerRating ?? booking.CustomerRating;
        booking.CustomerFeedback = request.CustomerFeedback ?? booking.CustomerFeedback;
        
        // Only set final price if explicitly provided (for backward compatibility)
        // Otherwise, it will be calculated automatically when completing the service
        if (request.FinalPrice.HasValue)
        {
            booking.FinalPrice = request.FinalPrice.Value;
        }
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

        if (!string.IsNullOrWhiteSpace(statusCode))
        {
            if (statusCode == BookingStatusCodes.Completed)
            {
                if (!booking.CompletedAt.HasValue)
                {
                    booking.CompletedAt = DateTime.UtcNow;
                }

                // Automatically calculate final price if not already set
                if (!booking.FinalPrice.HasValue && booking.EstimatedPrice.HasValue)
                {
                    // Calculate price after discount
                    decimal priceAfterDiscount = booking.EstimatedPrice.Value;
                    if (booking.DiscountAmount.HasValue && booking.DiscountAmount.Value > 0)
                    {
                        priceAfterDiscount = booking.EstimatedPrice.Value - booking.DiscountAmount.Value;
                        if (priceAfterDiscount < 0) priceAfterDiscount = 0;
                    }

                    // Use existing tax amounts if available, otherwise calculate them
                    decimal totalTaxAmount = booking.TotalTaxAmount ?? 0m;
                    decimal serviceChargeAmount = booking.ServiceChargeAmount ?? 0m;
                    decimal platformChargeAmount = booking.PlatformChargeAmount ?? 0m;

                    // If tax amounts are not stored, calculate them based on price after discount
                    if (totalTaxAmount == 0 && serviceChargeAmount == 0 && platformChargeAmount == 0)
                    {
                        // Get company configuration for customer state comparison
                        var companyConfig = await _context.CompanyConfigurations
                            .AsNoTracking()
                            .Include(cc => cc.CompanyState)
                            .FirstOrDefaultAsync(cc => cc.IsActive, cancellationToken);

                        // Get customer state from pincode
                        var pincodeEntry = await _context.CityPincodes
                            .AsNoTracking()
                            .Include(p => p.City)
                            .ThenInclude(c => c.State)
                            .FirstOrDefaultAsync(p => p.Pincode == booking.Pincode, cancellationToken);

                        var customerStateId = pincodeEntry?.City?.StateId;
                        var companyStateId = companyConfig?.CompanyStateId;
                        bool isSameState = customerStateId.HasValue && companyStateId.HasValue && customerStateId.Value == companyStateId.Value;

                        // Get all active tax rates from tax_master
                        var cgstTax = await _context.TaxMasters
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.IsActive && t.TaxName.ToUpper() == "CGST", cancellationToken);
                        var sgstTax = await _context.TaxMasters
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.IsActive && t.TaxName.ToUpper() == "SGST", cancellationToken);
                        var igstTax = await _context.TaxMasters
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.IsActive && t.TaxName.ToUpper() == "IGST", cancellationToken);
                        var serviceChargeTax = await _context.TaxMasters
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.IsActive && t.TaxName.ToUpper() == "SERVICE CHARGE", cancellationToken);
                        var platformChargeTax = await _context.TaxMasters
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.IsActive && t.TaxName.ToUpper() == "PLATFORM CHARGE", cancellationToken);

                        // Calculate taxes
                        decimal cgstAmount = 0m;
                        decimal sgstAmount = 0m;
                        decimal igstAmount = 0m;

                        if (isSameState)
                        {
                            // Same state: CGST + SGST
                            if (cgstTax != null)
                            {
                                cgstAmount = priceAfterDiscount * (cgstTax.TaxPercentage / 100m);
                                booking.CgstAmount = cgstAmount;
                            }
                            if (sgstTax != null)
                            {
                                sgstAmount = priceAfterDiscount * (sgstTax.TaxPercentage / 100m);
                                booking.SgstAmount = sgstAmount;
                            }
                            totalTaxAmount = cgstAmount + sgstAmount;
                        }
                        else
                        {
                            // Different state: IGST
                            if (igstTax != null)
                            {
                                igstAmount = priceAfterDiscount * (igstTax.TaxPercentage / 100m);
                                booking.IgstAmount = igstAmount;
                            }
                            totalTaxAmount = igstAmount;
                        }

                        // Calculate service charge
                        if (serviceChargeTax != null)
                        {
                            serviceChargeAmount = priceAfterDiscount * (serviceChargeTax.TaxPercentage / 100m);
                            booking.ServiceChargeAmount = serviceChargeAmount;
                        }

                        // Calculate platform charge
                        if (platformChargeTax != null)
                        {
                            platformChargeAmount = priceAfterDiscount * (platformChargeTax.TaxPercentage / 100m);
                            booking.PlatformChargeAmount = platformChargeAmount;
                        }

                        booking.TotalTaxAmount = totalTaxAmount;
                    }

                    // Calculate final price: price after discount + taxes + service charge + platform charge
                    booking.FinalPrice = priceAfterDiscount + totalTaxAmount + serviceChargeAmount + platformChargeAmount;
                }

                var currentAssignment = await _context.BookingAssignments
                    .FirstOrDefaultAsync(ba => ba.BookingRequestId == bookingId && ba.IsCurrent, cancellationToken);

                if (currentAssignment != null)
                {
                    currentAssignment.IsCurrent = false;
                    currentAssignment.UnassignedAt = DateTime.UtcNow;
                    currentAssignment.UnassignedReason = "Booking completed";
                    currentAssignment.ReasonType = "completed";
                }
            }
            else if (statusCode == BookingStatusCodes.Cancelled)
            {
                var currentAssignment = await _context.BookingAssignments
                    .FirstOrDefaultAsync(ba => ba.BookingRequestId == bookingId && ba.IsCurrent, cancellationToken);

                if (currentAssignment != null)
                {
                    currentAssignment.IsCurrent = false;
                    currentAssignment.UnassignedAt = DateTime.UtcNow;
                    currentAssignment.UnassignedReason = "Booking cancelled";
                    currentAssignment.ReasonType = "cancelled";
                }
            }
        }

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

        // Batch load all required data to avoid N+1 queries
        var serviceIds = recentBookings.Select(b => b.ServiceId).Distinct().ToList();
        var serviceTypeIds = recentBookings
            .Where(b => b.ServiceTypeId.HasValue)
            .Select(b => b.ServiceTypeId.Value)
            .Distinct()
            .ToList();
        
        // Collect all user IDs (customers, service providers, admins)
        var allUserIds = new HashSet<Guid>();
        foreach (var booking in recentBookings)
        {
            allUserIds.Add(booking.CustomerId);
            if (booking.ServiceProviderId.HasValue)
            {
                allUserIds.Add(booking.ServiceProviderId.Value);
            }
            if (booking.AdminId.HasValue)
            {
                allUserIds.Add(booking.AdminId.Value);
            }
        }

        // Load data sequentially to avoid concurrent DbContext operations
        var servicesDict = await _context.Services
            .AsNoTracking()
            .Where(s => serviceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        var serviceTypesDict = serviceTypeIds.Any()
            ? await _context.ServiceTypes
                .AsNoTracking()
                .Where(st => serviceTypeIds.Contains(st.Id))
                .ToDictionaryAsync(st => st.Id, cancellationToken)
            : new Dictionary<int, ServiceType>();

        var usersExtraDict = allUserIds.Any()
            ? await _context.UsersExtraInfos
                .AsNoTracking()
                .Where(info => info.UserId.HasValue && allUserIds.Contains(info.UserId.Value))
                .ToDictionaryAsync(info => info.UserId.Value, cancellationToken)
            : new Dictionary<Guid, UsersExtraInfo>();

        var dashboard = new CustomerDashboardResponse
        {
            UserPincode = primaryPincode,
            AvailableServices = servicesResponse.Services,
            RecentBookings = new List<BookingRequestDto>(),
            TotalBookings = totalBookings,
            PendingBookings = pendingBookings,
            CompletedBookings = completedBookings
        };

        // Build DTOs using pre-loaded data (no database queries in loop)
        foreach (var booking in recentBookings)
        {
            dashboard.RecentBookings.Add(BuildBookingDtoAsync(
                booking,
                servicesDict,
                serviceTypesDict,
                usersExtraDict));
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
            BookingStatusCodes.Pending,
            BookingStatusCodes.Assigned, 
            BookingStatusCodes.InProgress, 
            BookingStatusCodes.Completed 
        }, cancellationToken);
        var pendingStatusId = statusIds.GetValueOrDefault(BookingStatusCodes.Pending);
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

        // Week boundaries (Monday-Sunday) for KPI comparisons
        var utcNow = DateTime.UtcNow;
        var today = utcNow.Date;
        var daysFromMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var startOfCurrentWeek = today.AddDays(-daysFromMonday);
        var endOfCurrentWeek = startOfCurrentWeek.AddDays(7).AddTicks(-1);
        var startOfPreviousWeek = startOfCurrentWeek.AddDays(-7);
        var endOfPreviousWeek = startOfCurrentWeek.AddTicks(-1);

        // Total Job Volume: count of bookings assigned to provider in each week (by CreatedAt)
        var totalJobVolumeCurrent = await _context.BookingRequests
            .AsNoTracking()
            .CountAsync(b => b.ServiceProviderId == providerId.Value &&
                b.CreatedAt >= startOfCurrentWeek && b.CreatedAt <= endOfCurrentWeek, cancellationToken);
        var totalJobVolumePrev = await _context.BookingRequests
            .AsNoTracking()
            .CountAsync(b => b.ServiceProviderId == providerId.Value &&
                b.CreatedAt >= startOfPreviousWeek && b.CreatedAt <= endOfPreviousWeek, cancellationToken);
        response.TotalJobVolume = BuildKpiWithPercentChange(totalJobVolumeCurrent, totalJobVolumePrev);

        // Total Revenue: from InvoiceMaster for provider
        var totalRevenueCurrent = await _context.InvoiceMasters
            .AsNoTracking()
            .Where(i => i.ServiceProviderId == providerId.Value &&
                (i.InvoiceDate ?? i.CreatedAt) >= startOfCurrentWeek &&
                (i.InvoiceDate ?? i.CreatedAt) <= endOfCurrentWeek)
            .SumAsync(i => i.TotalAmount, cancellationToken);
        var totalRevenuePrev = await _context.InvoiceMasters
            .AsNoTracking()
            .Where(i => i.ServiceProviderId == providerId.Value &&
                (i.InvoiceDate ?? i.CreatedAt) >= startOfPreviousWeek &&
                (i.InvoiceDate ?? i.CreatedAt) <= endOfPreviousWeek)
            .SumAsync(i => i.TotalAmount, cancellationToken);
        response.TotalRevenue = BuildRevenueKpiWithPercentChange(totalRevenueCurrent, totalRevenuePrev);

        // Completed Jobs: count by CompletedAt in each week
        var completedCurrent = await _context.BookingRequests
            .AsNoTracking()
            .CountAsync(b => b.ServiceProviderId == providerId.Value && b.StatusId == completedStatusId &&
                b.CompletedAt.HasValue && b.CompletedAt.Value >= startOfCurrentWeek && b.CompletedAt.Value <= endOfCurrentWeek, cancellationToken);
        var completedPrev = await _context.BookingRequests
            .AsNoTracking()
            .CountAsync(b => b.ServiceProviderId == providerId.Value && b.StatusId == completedStatusId &&
                b.CompletedAt.HasValue && b.CompletedAt.Value >= startOfPreviousWeek && b.CompletedAt.Value <= endOfPreviousWeek, cancellationToken);
        response.CompletedJobs = BuildKpiWithPercentChange(completedCurrent, completedPrev);

        // Pending Jobs: assigned but not started (Assigned status) - "Awaiting Assignment"
        var pendingCurrent = await _context.BookingRequests
            .AsNoTracking()
            .CountAsync(b => b.ServiceProviderId == providerId.Value && b.StatusId == assignedStatusId &&
                b.CreatedAt >= startOfCurrentWeek && b.CreatedAt <= endOfCurrentWeek, cancellationToken);
        var pendingPrev = await _context.BookingRequests
            .AsNoTracking()
            .CountAsync(b => b.ServiceProviderId == providerId.Value && b.StatusId == assignedStatusId &&
                b.CreatedAt >= startOfPreviousWeek && b.CreatedAt <= endOfPreviousWeek, cancellationToken);
        response.PendingJobs = BuildKpiWithPercentChange(pendingCurrent, pendingPrev);

        // Today's Schedule: jobs with PreferredDate = today, ordered by PreferredTime (nulls last)
        var todaysScheduleBookings = await _context.BookingRequests
            .AsNoTracking()
            .Where(b => b.ServiceProviderId == providerId.Value &&
                b.PreferredDate.HasValue && b.PreferredDate.Value.Date == today &&
                (b.StatusId == assignedStatusId || b.StatusId == inProgressStatusId || b.StatusId == completedStatusId))
            .OrderBy(b => b.PreferredTime == null)
            .ThenBy(b => b.PreferredTime)
            .Take(10)
            .ToListAsync(cancellationToken);
        foreach (var booking in todaysScheduleBookings)
        {
            response.TodaysSchedule.Add(await BuildBookingDtoAsync(booking, cancellationToken));
        }

        // Weekly Revenue: daily breakdown for Mon-Sun of current week
        var dayNames = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        for (var i = 0; i < 7; i++)
        {
            var dayStart = startOfCurrentWeek.AddDays(i);
            var dayEnd = dayStart.AddDays(1).AddTicks(-1);
            var dayAmount = await _context.InvoiceMasters
                .AsNoTracking()
                .Where(inv => inv.ServiceProviderId == providerId.Value &&
                    (inv.InvoiceDate ?? inv.CreatedAt) >= dayStart &&
                    (inv.InvoiceDate ?? inv.CreatedAt) <= dayEnd)
                .SumAsync(inv => inv.TotalAmount, cancellationToken);
            response.WeeklyRevenue.Add(new WeeklyRevenueDayDto
            {
                Date = dayStart,
                DayName = dayNames[i],
                Amount = dayAmount
            });
        }

        // Your Performance: jobs completed this week, avg rating, % change vs previous week
        var jobsThisWeek = completedCurrent;
        var jobsPrevWeek = completedPrev;
        var completedWithRating = await _context.BookingRequests
            .AsNoTracking()
            .Where(b => b.ServiceProviderId == providerId.Value && b.StatusId == completedStatusId &&
                b.CompletedAt.HasValue && b.CompletedAt.Value >= startOfCurrentWeek && b.CompletedAt.Value <= endOfCurrentWeek &&
                b.CustomerRating.HasValue)
            .Select(b => b.CustomerRating!.Value)
            .ToListAsync(cancellationToken);
        var avgRating = completedWithRating.Count > 0 ? (decimal)completedWithRating.Average() : 0;
        var perfPercentChange = jobsPrevWeek > 0
            ? Math.Round((decimal)(jobsThisWeek - jobsPrevWeek) / jobsPrevWeek * 100, 1)
            : (jobsThisWeek > 0 ? 100m : 0m);
        response.YourPerformance = new ServiceProviderPerformanceDto
        {
            JobsThisPeriod = jobsThisWeek,
            JobsPreviousPeriod = jobsPrevWeek,
            PercentChange = perfPercentChange,
            AverageRating = Math.Round(avgRating, 1)
        };

        return ServiceResult.Ok(response);
    }

    private static ServiceProviderKpiDto BuildKpiWithPercentChange(int current, int previous)
    {
        var percentChange = previous > 0
            ? Math.Round((decimal)(current - previous) / previous * 100, 1)
            : (current > 0 ? 100m : 0m);
        return new ServiceProviderKpiDto { Current = current, Previous = previous, PercentChange = percentChange };
    }

    private static ServiceProviderRevenueKpiDto BuildRevenueKpiWithPercentChange(decimal current, decimal previous)
    {
        var percentChange = previous > 0
            ? Math.Round((current - previous) / previous * 100, 1)
            : (current > 0 ? 100m : 0m);
        return new ServiceProviderRevenueKpiDto { Current = current, Previous = previous, PercentChange = percentChange };
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
                }
            }

            var existingPreference = await _context.ServiceProviderPincodePreferences
                .FirstOrDefaultAsync(pref => pref.UserId == userId.Value && pref.Pincode == normalizedPincode, cancellationToken);

            if (existingPreference != null)
            {
                existingPreference.IsPrimary = request.IsPrimary;
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
                }
            }

            var existingPreference = await _context.CustomerPincodePreferences
                .FirstOrDefaultAsync(pref => pref.UserId == userId.Value && pref.Pincode == normalizedPincode, cancellationToken);

            if (existingPreference != null)
            {
                existingPreference.IsPrimary = request.IsPrimary;
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
        string? discountCode = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pincode))
        {
            return ServiceResult.BadRequest("Pincode is required.");
        }

        var normalizedPincode = pincode.Trim();
        var pincodeEntry = await _context.CityPincodes
            .AsNoTracking()
            .Include(cp => cp.City)
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

        // Get location price adjustment for this pincode
        var locationPriceAdjustment = await _context.LocationPriceAdjustments
            .AsNoTracking()
            .FirstOrDefaultAsync(lpa => lpa.IsActive && lpa.CityPincodeId == pincodeEntry.Id, cancellationToken);

        // Calculate price based on base price and location adjustment
        var basePriceValue = basePrice.HasValue ? basePrice.Value : 0m;
        var locationAdjustmentAmount = 0m;
        var calculatedPrice = basePriceValue;
        
        if (locationPriceAdjustment != null)
        {
            if (locationPriceAdjustment.PriceMultiplier.HasValue)
            {
                // If price multiplier exists, multiply the base price
                calculatedPrice = basePriceValue * locationPriceAdjustment.PriceMultiplier.Value;
                locationAdjustmentAmount = calculatedPrice - basePriceValue;
            }
            else if (locationPriceAdjustment.FixedAdjustment.HasValue)
            {
                // If price multiplier is null, add fixed adjustment
                locationAdjustmentAmount = locationPriceAdjustment.FixedAdjustment.Value;
                calculatedPrice = basePriceValue + locationAdjustmentAmount;
            }
        }

        // Apply discount if discount code is provided
        int? discountId = null;
        string? discountName = null;
        decimal discountAmount = 0m;
        decimal priceAfterDiscount = calculatedPrice;

        if (!string.IsNullOrWhiteSpace(discountCode))
        {
            var normalizedDiscountCode = discountCode.Trim().ToUpper();
            var discount = await _context.DiscountMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(d => 
                    d.IsActive 
                    && d.DiscountName != null 
                    && d.DiscountName.ToUpper() == normalizedDiscountCode
                    && d.ValidFrom <= DateTime.UtcNow
                    && d.ValidTo >= DateTime.UtcNow,
                    cancellationToken);

            if (discount != null)
            {
                // Check minimum order value
                if (calculatedPrice >= discount.MinOrderValue)
                {
                    discountId = discount.DiscountId;
                    discountName = discount.DiscountName;
                    
                    // Calculate discount amount based on discount type
                    if (discount.DiscountValue.HasValue)
                    {
                        if (discount.DiscountType?.ToLowerInvariant() == "percentage")
                        {
                            // Percentage discount
                            discountAmount = calculatedPrice * (discount.DiscountValue.Value / 100m);
                        }
                        else
                        {
                            // Fixed amount discount
                            discountAmount = discount.DiscountValue.Value;
                        }
                        
                        priceAfterDiscount = calculatedPrice - discountAmount;
                        
                        // Ensure price after discount doesn't go below zero
                        if (priceAfterDiscount < 0)
                        {
                            priceAfterDiscount = 0;
                            discountAmount = calculatedPrice;
                        }
                    }
                }
                else
                {
                    return ServiceResult.BadRequest($"Discount code '{discountCode}' requires a minimum order value of {discount.MinOrderValue}.");
                }
            }
            else
            {
                return ServiceResult.BadRequest($"Invalid or expired discount code '{discountCode}'.");
            }
        }

        // Get company configuration for customer state comparison
        var companyConfig = await _context.CompanyConfigurations
            .AsNoTracking()
            .Include(cc => cc.CompanyState)
            .FirstOrDefaultAsync(cc => cc.IsActive, cancellationToken);
        
        // Get customer state from pincode
        var customerStateId = pincodeEntry.City?.StateId;
        var companyStateId = companyConfig?.CompanyStateId;
        bool isSameState = customerStateId.HasValue && companyStateId.HasValue && customerStateId.Value == companyStateId.Value;

        // Get all active tax rates from tax_master
        var cgstTax = await _context.TaxMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsActive && t.TaxName.ToUpper() == "CGST", cancellationToken);
        var sgstTax = await _context.TaxMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsActive && t.TaxName.ToUpper() == "SGST", cancellationToken);
        var igstTax = await _context.TaxMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsActive && t.TaxName.ToUpper() == "IGST", cancellationToken);
        var serviceChargeTax = await _context.TaxMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsActive && t.TaxName.ToUpper() == "SERVICE CHARGE", cancellationToken);
        var platformChargeTax = await _context.TaxMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsActive && t.TaxName.ToUpper() == "PLATFORM CHARGE", cancellationToken);

        // Calculate taxes
        decimal cgstAmount = 0m;
        decimal sgstAmount = 0m;
        decimal igstAmount = 0m;
        decimal totalTaxAmount = 0m;

        if (isSameState)
        {
            // Same state: CGST + SGST (9% + 9% = 18%)
            if (cgstTax != null)
            {
                cgstAmount = priceAfterDiscount * (cgstTax.TaxPercentage / 100m);
            }
            if (sgstTax != null)
            {
                sgstAmount = priceAfterDiscount * (sgstTax.TaxPercentage / 100m);
            }
            totalTaxAmount = cgstAmount + sgstAmount;
        }
        else
        {
            // Different state: IGST (18%)
            if (igstTax != null)
            {
                igstAmount = priceAfterDiscount * (igstTax.TaxPercentage / 100m);
            }
            totalTaxAmount = igstAmount;
        }

        // Calculate service charge from tax_master (only if active)
        decimal serviceChargeAmount = 0m;
        if (serviceChargeTax != null)
        {
            serviceChargeAmount = priceAfterDiscount * (serviceChargeTax.TaxPercentage / 100m);
        }

        // Calculate platform charge from tax_master (only if active)
        decimal platformChargeAmount = 0m;
        if (platformChargeTax != null)
        {
            platformChargeAmount = priceAfterDiscount * (platformChargeTax.TaxPercentage / 100m);
        }

        // Calculate final price: price after discount + taxes + service charge + platform charge
        decimal finalPrice = priceAfterDiscount + totalTaxAmount + serviceChargeAmount + platformChargeAmount;

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
            BasePrice = basePriceValue,
            LocationAdjustmentAmount = locationAdjustmentAmount,
            CalculatedPrice = calculatedPrice,
            DiscountId = discountId,
            DiscountName = discountName,
            DiscountAmount = discountAmount,
            PriceAfterDiscount = priceAfterDiscount,
            CgstAmount = cgstAmount,
            SgstAmount = sgstAmount,
            IgstAmount = igstAmount,
            TotalTaxAmount = totalTaxAmount,
            ServiceChargeAmount = serviceChargeAmount,
            PlatformChargeAmount = platformChargeAmount,
            FinalPrice = finalPrice,
            IsSameState = isSameState
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

        var currentAssignment = await _context.BookingAssignments
            .FirstOrDefaultAsync(ba => ba.BookingRequestId == bookingId && ba.IsCurrent, cancellationToken);

        if (currentAssignment != null)
        {
            currentAssignment.IsCurrent = false;
            currentAssignment.UnassignedAt = DateTime.UtcNow;
            currentAssignment.UnassignedReason = "Booking cancelled by customer";
            currentAssignment.ReasonType = "cancelled";
        }

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

        // Get location price adjustment for this pincode
        var locationPriceAdjustment = await _context.LocationPriceAdjustments
            .AsNoTracking()
            .FirstOrDefaultAsync(lpa => lpa.IsActive && lpa.CityPincodeId == pincodeEntry.Id, cancellationToken);

        // Get base prices for all services (most recent effective price for each service)
        var serviceIds = services.Select(s => s.Id).ToList();
        var allServicePrices = await _context.ServicePrices
            .AsNoTracking()
            .Where(sp => sp.IsActive && sp.ServiceId.HasValue && serviceIds.Contains(sp.ServiceId.Value))
            .ToListAsync(cancellationToken);
        
        // Group by service ID and get the most recent price for each service
        var servicePricesDict = allServicePrices
            .GroupBy(sp => sp.ServiceId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(sp => sp.EffectiveFrom).First().Charges
            );

        response.Success = true;
        response.Pincode = pincode;
        response.Services = services.Select(service =>
        {
            // Get base price for this service
            var basePrice = servicePricesDict.TryGetValue(service.Id, out var price) ? price : 0m;
            var calculatedPrice = basePrice;
            var priceRating = 1m;

            // Apply location-based price adjustment
            if (locationPriceAdjustment != null)
            {
                if (locationPriceAdjustment.PriceMultiplier.HasValue)
                {
                    // If price multiplier exists, multiply the base price
                    calculatedPrice = basePrice * locationPriceAdjustment.PriceMultiplier.Value;
                    priceRating = locationPriceAdjustment.PriceMultiplier.Value;
                }
                else if (locationPriceAdjustment.FixedAdjustment.HasValue)
                {
                    // If price multiplier is null, add fixed adjustment
                    calculatedPrice = basePrice + locationPriceAdjustment.FixedAdjustment.Value;
                    priceRating = 1m; // Keep rating as 1 when using fixed adjustment
                }
            }

            return new ServiceAvailabilityItem
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
                CalculatedPrice = calculatedPrice,
                PriceRating = priceRating
            };
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
                    CategoryId = service.CategoryId ?? 0,
                    CategoryName = service.Category?.CategoryName
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
            .Include(s => s.Category)
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
                    CategoryId = service.CategoryId ?? 0,
                    CategoryName = service.Category?.CategoryName
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

    // Optimized version that uses pre-loaded data to avoid N+1 queries
    private BookingRequestDto BuildBookingDtoAsync(
        BookingRequest booking,
        Dictionary<int, Service> servicesDict,
        Dictionary<int, ServiceType> serviceTypesDict,
        Dictionary<Guid, UsersExtraInfo> usersExtraDict)
    {
        var service = servicesDict.GetValueOrDefault(booking.ServiceId);
        var serviceType = booking.ServiceTypeId.HasValue 
            ? serviceTypesDict.GetValueOrDefault(booking.ServiceTypeId.Value) 
            : null;

        BookingUserDto? MapUser(Guid userId)
        {
            var info = usersExtraDict.GetValueOrDefault(userId);
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
            var info = usersExtraDict.GetValueOrDefault(adminId);
            return info == null
                ? new BookingAdminDto { Id = adminId }
                : new BookingAdminDto
                {
                    Id = adminId,
                    Name = info.FullName,
                    Email = info.Email
                };
        }

        var serviceProviderInfo = booking.ServiceProviderId.HasValue
            ? usersExtraDict.GetValueOrDefault(booking.ServiceProviderId.Value)
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
                    CategoryId = service.CategoryId ?? 0,
                    CategoryName = service.Category?.CategoryName
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

    public async Task<ServiceResult> GetAvailableServiceProvidersAsync(int serviceId, string pincode, DateTime? preferredDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pincode))
            {
                return ServiceResult.BadRequest("Pincode is required.");
            }

            var normalizedPincode = pincode.Trim();
            var approvedStatusId = (int)VerificationStatusEnum.Approved;
            var serviceProviderRoleId = (int)RoleEnum.ServiceProvider;
            var checkDate = preferredDate?.Date ?? DateTime.UtcNow.Date;

            // Get service providers who:
            // 1. Offer this service (ProviderService)
            // 2. Serve this pincode (ServiceProviderPincodePreference)
            // 3. Are verified/approved (User.VerificationStatusId == Approved)
            // 4. Are active
            // 5. Are NOT on leave for the check date

            var providerIds = await (
                from ps in _context.ProviderServices
                join pref in _context.ServiceProviderPincodePreferences
                    on ps.UserId equals pref.UserId
                join user in _context.Users
                    on ps.UserId equals user.Id
                where ps.ServiceId == serviceId
                    && ps.IsActive
                    && pref.Pincode == normalizedPincode
                    && user.VerificationStatusId == approvedStatusId
                    && user.RoleId == serviceProviderRoleId
                select ps.UserId
            ).Distinct().ToListAsync(cancellationToken);

            // If no providers found with verification, try to get providers without verification check
            //if (providerIds.Count == 0)
            //{
            //    providerIds = await (
            //        from ps in _context.ProviderServices
            //        join pref in _context.ServiceProviderPincodePreferences
            //            on ps.UserId equals pref.UserId
            //        join user in _context.Users
            //            on ps.UserId equals user.Id
            //        where ps.ServiceId == serviceId
            //            && ps.IsActive
            //            && pref.Pincode == normalizedPincode
            //            && user.RoleId == serviceProviderRoleId
            //        select ps.UserId
            //    ).Distinct().ToListAsync(cancellationToken);
            //}

            // Filter out providers on leave
            var providersOnLeave = await _context.ServiceProviderLeaveDays
                .Where(l => providerIds.Contains(l.ServiceProviderId)
                    && l.LeaveDate == checkDate)
                .Select(l => l.ServiceProviderId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var availableProviderIds = providerIds.Except(providersOnLeave).ToList();

            if (availableProviderIds.Count == 0)
            {
                var emptyResponse = new AvailableServiceProvidersResponse
                {
                    Success = true,
                    ServiceProviders = new List<AvailableServiceProvider>(),
                    Message = "No service providers available for this service in the specified pincode"
                };
                return ServiceResult.Ok(emptyResponse);
            }

            // Get provider details with ratings and stats
            var providersWithDetails = await _context.UsersExtraInfos
                .Where(u => u.UserId.HasValue && availableProviderIds.Contains(u.UserId.Value))
                .Select(u => new
                {
                    ProviderId = u.UserId!.Value,
                    Name = u.FullName,
                    Email = u.Email,
                    Phone = u.PhoneNumber
                })
                .ToListAsync(cancellationToken);

            // Get ratings for each provider
            var ratings = await _context.Ratings
                .Where(r => availableProviderIds.Contains(r.RatingTo))
                .GroupBy(r => r.RatingTo)
                .Select(g => new
                {
                    ProviderId = g.Key,
                    AverageRating = g.Average(r => (decimal?)r.RatingValue) ?? 0,
                    TotalRatings = g.Count()
                })
                .ToListAsync(cancellationToken);

            var ratingsDict = ratings.ToDictionary(r => r.ProviderId, r => new { r.AverageRating, r.TotalRatings });

            // Get booking stats for each provider
            var pendingStatusId = await GetStatusIdByCodeAsync(BookingStatusCodes.Pending, cancellationToken);
            var inProgressStatusId = await GetStatusIdByCodeAsync(BookingStatusCodes.InProgress, cancellationToken);
            var completedStatusId = await GetStatusIdByCodeAsync(BookingStatusCodes.Completed, cancellationToken);
            var assignedStatusId = await GetStatusIdByCodeAsync(BookingStatusCodes.Assigned, cancellationToken);

            var bookingStats = await _context.BookingRequests
                .Where(b => b.ServiceProviderId.HasValue && availableProviderIds.Contains(b.ServiceProviderId.Value))
                .GroupBy(b => b.ServiceProviderId!.Value)
                .Select(g => new
                {
                    ProviderId = g.Key,
                    ActiveRequests = g.Count(b => b.StatusId == inProgressStatusId || b.StatusId == assignedStatusId),
                    PendingRequests = g.Count(b => b.StatusId == pendingStatusId),
                    CompletedRequests = g.Count(b => b.StatusId == completedStatusId)
                })
                .ToListAsync(cancellationToken);

            var statsDict = bookingStats.ToDictionary(s => s.ProviderId, s => new { s.ActiveRequests, s.PendingRequests, s.CompletedRequests });

            // Build response
            var availableProviders = providersWithDetails.Select(p => new AvailableServiceProvider
            {
                Id = p.ProviderId,
                Name = p.Name,
                Email = p.Email,
                Phone = string.IsNullOrWhiteSpace(p.Phone) ? null : p.Phone,
                AverageRating = ratingsDict.ContainsKey(p.ProviderId) ? ratingsDict[p.ProviderId].AverageRating : null,
                TotalRatings = ratingsDict.ContainsKey(p.ProviderId) ? ratingsDict[p.ProviderId].TotalRatings : 0,
                ActiveRequests = statsDict.ContainsKey(p.ProviderId) ? statsDict[p.ProviderId].ActiveRequests : 0,
                PendingRequests = statsDict.ContainsKey(p.ProviderId) ? statsDict[p.ProviderId].PendingRequests : 0,
                CompletedRequests = statsDict.ContainsKey(p.ProviderId) ? statsDict[p.ProviderId].CompletedRequests : 0
            })
            .OrderByDescending(p => p.AverageRating ?? 0)
            .ThenByDescending(p => p.CompletedRequests)
            .ToList();

            var providerCount = availableProviders.Count;
            var finalResponse = new AvailableServiceProvidersResponse
            {
                Success = true,
                ServiceProviders = availableProviders,
                Message = providerCount > 0 
                    ? $"Found {providerCount} available service provider(s)" 
                    : "No service providers available for this service in the specified pincode"
            };

            return ServiceResult.Ok(finalResponse);
        }
        catch (Exception ex)
        {
            return new ServiceResult
            {
                StatusCode = System.Net.HttpStatusCode.InternalServerError,
                Payload = new AvailableServiceProvidersResponse
                {
                    Success = false,
                    Message = $"An error occurred while retrieving available service providers: {ex.Message}",
                    ServiceProviders = new List<AvailableServiceProvider>()
                }
            };
        }
    }

    /// <summary>
    /// Gets the default admin ID. Returns null if no default admin exists.
    /// </summary>
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
}
