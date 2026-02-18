using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using APIServiceManagement.Application.Interfaces.Services;
using APIServiceManagement.Domain.Constants;
using APIServiceManagement.Domain.Entities;
using APIServiceManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Infrastructure.Services;

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _context;
    private readonly IPdfGenerationService _pdfGenerationService;
    private readonly INotificationService _notificationService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        AppDbContext context,
        IPdfGenerationService pdfGenerationService,
        INotificationService notificationService,
        IFileStorageService fileStorageService,
        ILogger<InvoiceService> logger)
    {
        _context = context;
        _pdfGenerationService = pdfGenerationService;
        _notificationService = notificationService;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public async Task<ServiceResult> GenerateInvoiceAsync(Guid bookingId, GenerateInvoiceRequest? request, CancellationToken cancellationToken = default)
    {
        // Use execution strategy to support retry logic
        // Note: We don't use explicit transactions with retry strategies - SaveChangesAsync handles transactions implicitly
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            try
            {
                // Load booking with all related entities
                var booking = await _context.BookingRequests
                .Include(b => b.Customer)
                .Include(b => b.ServiceProvider)
                .Include(b => b.Service)
                .Include(b => b.StatusNavigation)
                .Include(b => b.Discount)
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

                if (booking == null)
                {
                    return ServiceResult.NotFound(new MessageResponse { Message = "Booking not found." });
                }

                // Validate that customer exists in users table
                var customerExists = await _context.Users
                    .AnyAsync(u => u.Id == booking.CustomerId, cancellationToken);
                
                if (!customerExists)
                {
                    _logger.LogError("Customer {CustomerId} from booking {BookingId} does not exist in users table", booking.CustomerId, bookingId);
                    return ServiceResult.BadRequest($"Customer associated with this booking does not exist. Cannot generate invoice.");
                }

                // Validate that service exists
                var serviceExists = await _context.Services
                    .AnyAsync(s => s.Id == booking.ServiceId, cancellationToken);
                
                if (!serviceExists)
                {
                    _logger.LogError("Service {ServiceId} from booking {BookingId} does not exist", booking.ServiceId, bookingId);
                    return ServiceResult.BadRequest($"Service associated with this booking does not exist. Cannot generate invoice.");
                }

                // Validate service provider exists if provided
                Guid? validServiceProviderId = null;
                if (booking.ServiceProviderId.HasValue)
                {
                    var serviceProviderExists = await _context.Users
                        .AnyAsync(u => u.Id == booking.ServiceProviderId.Value, cancellationToken);
                    
                    if (serviceProviderExists)
                    {
                        validServiceProviderId = booking.ServiceProviderId.Value;
                    }
                    else
                    {
                        _logger.LogWarning("Service provider {ServiceProviderId} from booking {BookingId} does not exist, invoice will be created without service provider", booking.ServiceProviderId.Value, bookingId);
                    }
                }

                // Validate booking has required pricing information
                if (!booking.BasePrice.HasValue && !booking.EstimatedPrice.HasValue && !booking.FinalPrice.HasValue)
                {
                    return ServiceResult.BadRequest("Booking does not have pricing information. Cannot generate invoice.");
                }

                // Ensure booking is marked as completed
                var completedStatusId = await GetStatusIdByCodeAsync(BookingStatusCodes.Completed, cancellationToken);
                if (booking.StatusId != completedStatusId)
                {
                    // Update booking status to completed
                    booking.StatusId = completedStatusId;
                    booking.Status = BookingStatusCodes.Completed;
                    if (!booking.CompletedAt.HasValue)
                    {
                        booking.CompletedAt = DateTime.UtcNow;
                    }
                    booking.UpdatedAt = DateTime.UtcNow;
                    
                    // Save the status update
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Booking {BookingId} status updated to Completed during invoice generation", bookingId);
                }

                // Check if invoice already exists
                var existingInvoice = await _context.InvoiceMasters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.BookingId == bookingId, cancellationToken);

                if (existingInvoice != null)
                {
                    _logger.LogWarning("Invoice already exists for booking {BookingId}. Invoice ID: {InvoiceId}", bookingId, existingInvoice.InvoiceId);
                    return ServiceResult.BadRequest(new MessageResponse { Message = $"Invoice already exists for this booking. Invoice Number: {existingInvoice.InvoiceNumber}" });
                }

                // Get company configuration for invoice settings
                var companyConfig = await _context.CompanyConfigurations
                    .Include(c => c.CompanyState)
                    .Where(c => c.IsActive)
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                var invoicePrefix = companyConfig?.InvoicePrefix ?? "INV";
                var paymentTermsDays = companyConfig?.PaymentTermsDays ?? 30;

                // Generate invoice number
                var invoiceNumber = await GenerateInvoiceNumberAsync(invoicePrefix, cancellationToken);

                // Calculate base amounts from booking
                var basePrice = booking.BasePrice ?? booking.FinalPrice ?? booking.EstimatedPrice ?? 0;
                var locationAdjustment = booking.LocationAdjustmentAmount ?? 0;
                var serviceCharge = booking.ServiceChargeAmount ?? 0;
                var platformCharge = booking.PlatformChargeAmount ?? 0;

                // Calculate subtotal (base + adjustments + charges)
                var subtotal = basePrice + locationAdjustment + serviceCharge + platformCharge;

                // Handle discounts
                decimal totalDiscountAmount = 0;
                if (booking.DiscountAmount.HasValue && booking.DiscountAmount.Value > 0)
                {
                    totalDiscountAmount = booking.DiscountAmount.Value;
                    subtotal -= totalDiscountAmount;
                }

                // Handle add-ons (from booking if any - you can extend this)
                decimal totalAddonAmount = 0;
                // Add-ons can be added here if booking has addon information

                // Calculate taxable amount (subtotal after discounts and addons)
                var taxableAmount = subtotal + totalAddonAmount;

                // Handle taxes - create invoice_taxes records
                decimal totalTaxAmount = 0;
                if (booking.TotalTaxAmount.HasValue && booking.TotalTaxAmount.Value > 0)
                {
                    totalTaxAmount = booking.TotalTaxAmount.Value;

                    // Create tax records for CGST, SGST, IGST if they exist
                    if (booking.CgstAmount.HasValue && booking.CgstAmount.Value > 0)
                    {
                        var cgstTax = await GetOrCreateTaxAsync("CGST", cancellationToken);
                        // Calculate CGST percentage from amount
                        var cgstPercentage = taxableAmount > 0 ? (booking.CgstAmount.Value / taxableAmount) * 100 : 0;
                        // We'll create the invoice tax record below
                    }

                    if (booking.SgstAmount.HasValue && booking.SgstAmount.Value > 0)
                    {
                        var sgstTax = await GetOrCreateTaxAsync("SGST", cancellationToken);
                        // Calculate SGST percentage from amount
                        var sgstPercentage = taxableAmount > 0 ? (booking.SgstAmount.Value / taxableAmount) * 100 : 0;
                    }

                    if (booking.IgstAmount.HasValue && booking.IgstAmount.Value > 0)
                    {
                        var igstTax = await GetOrCreateTaxAsync("IGST", cancellationToken);
                        // Calculate IGST percentage from amount
                        var igstPercentage = taxableAmount > 0 ? (booking.IgstAmount.Value / taxableAmount) * 100 : 0;
                    }
                }

                // Calculate final total
                var totalAmount = taxableAmount + totalTaxAmount;

                // Validate total amount is positive
                if (totalAmount <= 0)
                {
                    return ServiceResult.BadRequest("Total invoice amount must be greater than zero.");
                }

                // Create invoice master
                var invoice = new InvoiceMaster
                {
                    InvoiceNumber = invoiceNumber,
                    BookingId = booking.Id,
                    CustomerId = booking.CustomerId,
                    ServiceProviderId = validServiceProviderId,
                    ServiceId = booking.ServiceId,
                    InvoiceDate = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(paymentTermsDays),
                    BasePrice = basePrice,
                    LocationAdjustment = locationAdjustment > 0 ? locationAdjustment : null,
                    Subtotal = subtotal,
                    TotalTaxAmount = totalTaxAmount,
                    TotalDiscountAmount = totalDiscountAmount,
                    TotalAddonAmount = totalAddonAmount,
                    TotalAmount = totalAmount,
                    FinalAmount = totalAmount, // Keep for backward compatibility
                    PaymentStatus = "Pending",
                    Status = "Pending", // Keep for backward compatibility
                    Notes = request?.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Save invoice first to get the InvoiceId
                _context.InvoiceMasters.Add(invoice);
                await _context.SaveChangesAsync(cancellationToken);

                // Add discount record if exists
                if (totalDiscountAmount > 0 && booking.DiscountId.HasValue)
                {
                    var discount = booking.Discount;
                    invoice.InvoiceDiscounts.Add(new InvoiceDiscount
                    {
                        InvoiceId = invoice.InvoiceId,
                        DiscountId = booking.DiscountId.Value,
                        DiscountName = discount?.DiscountName,
                        DiscountType = discount?.DiscountType ?? "Fixed",
                        DiscountValue = discount?.DiscountValue ?? totalDiscountAmount,
                        DiscountAmount = totalDiscountAmount
                    });
                }
                else if (totalDiscountAmount > 0)
                {
                    // Discount without master record - we need a discount_id, so skip for now
                    // You may want to create a default discount in discount_master
                    _logger.LogWarning("Discount amount exists but no discount_id. Skipping invoice_discounts record.");
                }

                // Add tax records
                if (booking.CgstAmount.HasValue && booking.CgstAmount.Value > 0)
                {
                    var cgstTax = await GetOrCreateTaxAsync("CGST", cancellationToken);
                    var cgstPercentage = taxableAmount > 0 ? (booking.CgstAmount.Value / taxableAmount) * 100 : 0;
                    invoice.InvoiceTaxes.Add(new InvoiceTax
                    {
                        InvoiceId = invoice.InvoiceId,
                        TaxId = cgstTax.TaxId,
                        TaxName = "CGST",
                        TaxPercentage = cgstPercentage,
                        TaxableAmount = taxableAmount,
                        TaxAmount = booking.CgstAmount.Value
                    });
                }

                if (booking.SgstAmount.HasValue && booking.SgstAmount.Value > 0)
                {
                    var sgstTax = await GetOrCreateTaxAsync("SGST", cancellationToken);
                    var sgstPercentage = taxableAmount > 0 ? (booking.SgstAmount.Value / taxableAmount) * 100 : 0;
                    invoice.InvoiceTaxes.Add(new InvoiceTax
                    {
                        InvoiceId = invoice.InvoiceId,
                        TaxId = sgstTax.TaxId,
                        TaxName = "SGST",
                        TaxPercentage = sgstPercentage,
                        TaxableAmount = taxableAmount,
                        TaxAmount = booking.SgstAmount.Value
                    });
                }

                if (booking.IgstAmount.HasValue && booking.IgstAmount.Value > 0)
                {
                    var igstTax = await GetOrCreateTaxAsync("IGST", cancellationToken);
                    var igstPercentage = taxableAmount > 0 ? (booking.IgstAmount.Value / taxableAmount) * 100 : 0;
                    invoice.InvoiceTaxes.Add(new InvoiceTax
                    {
                        InvoiceId = invoice.InvoiceId,
                        TaxId = igstTax.TaxId,
                        TaxName = "IGST",
                        TaxPercentage = igstPercentage,
                        TaxableAmount = taxableAmount,
                        TaxAmount = booking.IgstAmount.Value
                    });
                }

                // Note: Add-ons are handled differently in existing structure
                // They reference add_on_master via add_on_id
                // For now, we'll store location adjustment, service charge, and platform charge
                // in the invoice_master columns (location_adjustment, etc.)
                // If you need to create add_on records, you'll need add_on_id from add_on_master

                // Save related records (taxes, discounts)
                await _context.SaveChangesAsync(cancellationToken);

                // Generate PDF with company configuration
                var invoiceResponse = await MapToInvoiceResponseAsync(invoice, cancellationToken);
                byte[] pdfBytes;
                string pdfPath;

                try
                {
                    pdfBytes = await _pdfGenerationService.GenerateInvoicePdfAsync(invoiceResponse, cancellationToken);
                    
                    // Save PDF to storage
                    var pdfFileName = $"invoice_{invoiceNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                    pdfPath = await _fileStorageService.SaveFileAsync(pdfBytes, pdfFileName, "invoices", cancellationToken);
                    
                    // Update invoice with PDF path
                    invoice.PdfPath = pdfPath;
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception pdfEx)
                {
                    _logger.LogError(pdfEx, "Error generating PDF for invoice {InvoiceNumber}. Invoice will be saved without PDF.", invoiceNumber);
                    // Continue without PDF - invoice is still valid
                    pdfPath = null;
                }

                _logger.LogInformation("Invoice {InvoiceNumber} generated successfully for booking {BookingId}. Total Amount: {TotalAmount}", 
                    invoiceNumber, bookingId, totalAmount);

                return ServiceResult.Created(invoiceResponse);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error generating invoice for booking {BookingId}. Inner exception: {InnerException}", bookingId, dbEx.InnerException?.Message);
                
                // Check for foreign key constraint violations
                if (dbEx.InnerException != null && dbEx.InnerException.Message.Contains("foreign key constraint"))
                {
                    string errorMessage = "Foreign key constraint violation: ";
                    if (dbEx.InnerException.Message.Contains("customer_id"))
                    {
                        errorMessage += "Customer does not exist in the system.";
                    }
                    else if (dbEx.InnerException.Message.Contains("service_id"))
                    {
                        errorMessage += "Service does not exist in the system.";
                    }
                    else if (dbEx.InnerException.Message.Contains("service_provider_id"))
                    {
                        errorMessage += "Service provider does not exist in the system.";
                    }
                    else if (dbEx.InnerException.Message.Contains("booking_id"))
                    {
                        errorMessage += "Booking reference is invalid.";
                    }
                    else
                    {
                        errorMessage += dbEx.InnerException.Message;
                    }
                    
                    return ServiceResult.BadRequest(errorMessage);
                }
                
                // Check if invoice was partially created and clean up
                try
                {
                    var partialInvoice = await _context.InvoiceMasters
                        .FirstOrDefaultAsync(i => i.BookingId == bookingId, cancellationToken);
                    if (partialInvoice != null)
                    {
                        _context.InvoiceMasters.Remove(partialInvoice);
                        await _context.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("Cleaned up partially created invoice for booking {BookingId}", bookingId);
                    }
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "Error cleaning up partial invoice for booking {BookingId}", bookingId);
                }
                
                return ServiceResult.BadRequest($"Database error occurred while generating invoice: {dbEx.InnerException?.Message ?? dbEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice for booking {BookingId}", bookingId);
                
                // Check if invoice was partially created and clean up
                try
                {
                    var partialInvoice = await _context.InvoiceMasters
                        .FirstOrDefaultAsync(i => i.BookingId == bookingId, cancellationToken);
                    if (partialInvoice != null)
                    {
                        _context.InvoiceMasters.Remove(partialInvoice);
                        await _context.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("Cleaned up partially created invoice for booking {BookingId}", bookingId);
                    }
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "Error cleaning up partial invoice for booking {BookingId}", bookingId);
                }
                
                return ServiceResult.BadRequest($"Error generating invoice: {ex.Message}");
            }
        });
    }

    public async Task<ServiceResult> GetInvoiceByIdAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.InvoiceMasters
            .Include(i => i.Booking)
                .ThenInclude(b => b.Service)
            .Include(i => i.Booking)
                .ThenInclude(b => b.Customer)
            .Include(i => i.Booking)
                .ThenInclude(b => b.ServiceProvider)
            .Include(i => i.InvoiceTaxes)
                .ThenInclude(t => t.Tax)
            .Include(i => i.InvoiceDiscounts)
                .ThenInclude(d => d.Discount)
            .Include(i => i.InvoiceAddOns)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId, cancellationToken);

        if (invoice == null)
        {
            return ServiceResult.NotFound(new MessageResponse { Message = "Invoice not found." });
        }

        var response = await MapToInvoiceResponseAsync(invoice, cancellationToken);
        return ServiceResult.Ok(response);
    }

    public async Task<ServiceResult> GetInvoiceByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.InvoiceMasters
            .Include(i => i.Booking)
                .ThenInclude(b => b.Service)
            .Include(i => i.Booking)
                .ThenInclude(b => b.Customer)
            .Include(i => i.Booking)
                .ThenInclude(b => b.ServiceProvider)
            .Include(i => i.InvoiceTaxes)
                .ThenInclude(t => t.Tax)
            .Include(i => i.InvoiceDiscounts)
                .ThenInclude(d => d.Discount)
            .Include(i => i.InvoiceAddOns)
            .FirstOrDefaultAsync(i => i.BookingId == bookingId, cancellationToken);

        if (invoice == null)
        {
            return ServiceResult.NotFound(new MessageResponse { Message = "Invoice not found for this booking." });
        }

        var response = await MapToInvoiceResponseAsync(invoice, cancellationToken);
        return ServiceResult.Ok(response);
    }

    public async Task<ServiceResult> GetInvoiceByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.InvoiceMasters
            .Include(i => i.Booking)
                .ThenInclude(b => b.Service)
            .Include(i => i.Booking)
                .ThenInclude(b => b.Customer)
            .Include(i => i.Booking)
                .ThenInclude(b => b.ServiceProvider)
            .Include(i => i.InvoiceTaxes)
                .ThenInclude(t => t.Tax)
            .Include(i => i.InvoiceDiscounts)
                .ThenInclude(d => d.Discount)
            .Include(i => i.InvoiceAddOns)
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, cancellationToken);

        if (invoice == null)
        {
            return ServiceResult.NotFound(new MessageResponse { Message = "Invoice not found." });
        }

        var response = await MapToInvoiceResponseAsync(invoice, cancellationToken);
        return ServiceResult.Ok(response);
    }

    public async Task<ServiceResult> GetInvoicePdfAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.InvoiceMasters
            .Include(i => i.Booking)
                .ThenInclude(b => b.Service)
            .Include(i => i.Booking)
                .ThenInclude(b => b.Customer)
            .Include(i => i.Booking)
                .ThenInclude(b => b.ServiceProvider)
            .Include(i => i.InvoiceTaxes)
                .ThenInclude(t => t.Tax)
            .Include(i => i.InvoiceDiscounts)
                .ThenInclude(d => d.Discount)
            .Include(i => i.InvoiceAddOns)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId, cancellationToken);

        if (invoice == null)
        {
            return ServiceResult.NotFound(new MessageResponse { Message = "Invoice not found." });
        }

        try
        {
            byte[] pdfBytes;

            // If PDF exists in storage, retrieve it
            if (!string.IsNullOrEmpty(invoice.PdfPath))
            {
                pdfBytes = await _fileStorageService.GetFileAsync(invoice.PdfPath, cancellationToken);
            }
            else
            {
                // Generate PDF on the fly
                var invoiceResponse = await MapToInvoiceResponseAsync(invoice, cancellationToken);
                pdfBytes = await _pdfGenerationService.GenerateInvoicePdfAsync(invoiceResponse, cancellationToken);
            }
            string invoidNumberAsString=string.IsNullOrEmpty(invoice.InvoiceNumber) ? invoiceId.ToString() : invoice.InvoiceNumber;

            var response = new InvoicePdfResponse
            {
                PdfBytes = pdfBytes,
                InvoiceNumber = invoice.InvoiceNumber ?? string.Empty,
                FileName = $"invoice_{invoidNumberAsString}.pdf",
                ContentType = "application/pdf"
            };

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PDF for invoice {InvoiceId}", invoiceId);
            return ServiceResult.BadRequest($"Error retrieving PDF: {ex.Message}");
        }
    }

    public async Task<ServiceResult> GetInvoicePdfByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.InvoiceMasters
            .Include(i => i.Booking)
                .ThenInclude(b => b.Service)
            .Include(i => i.Booking)
                .ThenInclude(b => b.Customer)
            .Include(i => i.Booking)
                .ThenInclude(b => b.ServiceProvider)
            .Include(i => i.InvoiceTaxes)
                .ThenInclude(t => t.Tax)
            .Include(i => i.InvoiceDiscounts)
                .ThenInclude(d => d.Discount)
            .Include(i => i.InvoiceAddOns)
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, cancellationToken);

        if (invoice == null)
        {
            return ServiceResult.NotFound(new MessageResponse { Message = "Invoice not found." });
        }

        try
        {
            byte[] pdfBytes;

            // If PDF exists in storage, retrieve it
            if (!string.IsNullOrEmpty(invoice.PdfPath))
            {
                pdfBytes = await _fileStorageService.GetFileAsync(invoice.PdfPath, cancellationToken);
            }
            else
            {
                // Generate PDF on the fly
                var invoiceResponse = await MapToInvoiceResponseAsync(invoice, cancellationToken);
                pdfBytes = await _pdfGenerationService.GenerateInvoicePdfAsync(invoiceResponse, cancellationToken);
            }

            var response = new InvoicePdfResponse
            {
                PdfBytes = pdfBytes,
                InvoiceNumber = invoiceNumber,
                FileName = $"invoice_{invoiceNumber}.pdf",
                ContentType = "application/pdf"
            };

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PDF for invoice {InvoiceNumber}", invoiceNumber);
            return ServiceResult.BadRequest($"Error retrieving PDF: {ex.Message}");
        }
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

    private async Task<string> GenerateInvoiceNumberAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow;
        var invoicePrefix = $"{prefix}-{today:yyyyMMdd}-";
        
        // Try to get the last invoice with the same prefix for today
        var lastInvoice = await _context.InvoiceMasters
            .AsNoTracking()
            .Where(i => i.InvoiceNumber.StartsWith(invoicePrefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        int sequence = 1;
        if (lastInvoice != null)
        {
            var lastSequence = lastInvoice.InvoiceNumber.Replace(invoicePrefix, "");
            if (int.TryParse(lastSequence, out var lastSeq))
            {
                sequence = lastSeq + 1;
            }
        }

        // Ensure uniqueness by checking if invoice number already exists
        string invoiceNumber;
        int maxAttempts = 100;
        int attempts = 0;
        do
        {
            invoiceNumber = $"{invoicePrefix}{sequence:D4}";
            var exists = await _context.InvoiceMasters
                .AsNoTracking()
                .AnyAsync(i => i.InvoiceNumber == invoiceNumber, cancellationToken);
            
            if (!exists)
                break;
                
            sequence++;
            attempts++;
        } while (attempts < maxAttempts);

        if (attempts >= maxAttempts)
        {
            throw new InvalidOperationException("Unable to generate unique invoice number after multiple attempts.");
        }

        return invoiceNumber;
    }

    private async Task<TaxMaster> GetOrCreateTaxAsync(string taxName, CancellationToken cancellationToken = default)
    {
        var tax = await _context.TaxMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TaxName == taxName && t.IsActive, cancellationToken);

        if (tax == null)
        {
            // Create default tax if not exists (you may want to handle this differently)
            _logger.LogWarning("Tax {TaxName} not found in tax_master. Using default values.", taxName);
            // Return a temporary tax object - in production, you should ensure taxes exist
            return new TaxMaster
            {
                TaxId = 0,
                TaxName = taxName,
                TaxPercentage = 0,
                IsActive = true
            };
        }

        return tax;
    }

    private async Task<InvoiceResponse> MapToInvoiceResponseAsync(InvoiceMaster invoice, CancellationToken cancellationToken = default)
    {
        // Ensure related entities are loaded
        if (invoice.Booking == null)
        {
            invoice.Booking = await _context.BookingRequests
                .Include(b => b.Service)
                .Include(b => b.Customer)
                .Include(b => b.ServiceProvider)
                .FirstOrDefaultAsync(b => b.Id == invoice.BookingId, cancellationToken);
        }

        // Load company configuration
        var companyConfig = await _context.CompanyConfigurations
            .Include(c => c.CompanyState)
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        // Calculate tax amounts from invoice_taxes
        var cgstAmount = invoice.InvoiceTaxes.FirstOrDefault(t => t.TaxName == "CGST")?.TaxAmount;
        var sgstAmount = invoice.InvoiceTaxes.FirstOrDefault(t => t.TaxName == "SGST")?.TaxAmount;
        var igstAmount = invoice.InvoiceTaxes.FirstOrDefault(t => t.TaxName == "IGST")?.TaxAmount;

        // Use BasePrice from invoice_master, fallback to calculated value
        var basePrice = invoice.BasePrice > 0 ? invoice.BasePrice : (invoice.Subtotal - invoice.TotalAddonAmount);

        // Get location adjustment, service charge, platform charge from invoice_master or addons
        var locationAdjustment = invoice.LocationAdjustment ?? 
            invoice.InvoiceAddOns?.FirstOrDefault(a => a.AddonName == "Location Adjustment")?.TotalPrice ?? 
            invoice.InvoiceAddOns?.FirstOrDefault(a => a.AddonName == "Location Adjustment")?.TotalAmount;
        
        var serviceCharge = invoice.InvoiceAddOns?.FirstOrDefault(a => a.AddonName == "Service Charge")?.TotalPrice ?? 
            invoice.InvoiceAddOns?.FirstOrDefault(a => a.AddonName == "Service Charge")?.TotalAmount;
        
        var platformCharge = invoice.InvoiceAddOns?.FirstOrDefault(a => a.AddonName == "Platform Charge")?.TotalPrice ?? 
            invoice.InvoiceAddOns?.FirstOrDefault(a => a.AddonName == "Platform Charge")?.TotalAmount;

        return new InvoiceResponse
        {
            Id = invoice.InvoiceId,
            InvoiceNumber = invoice.InvoiceNumber,
            BookingId = invoice.BookingId,
            CustomerId = invoice.CustomerId,
            ServiceProviderId = invoice.ServiceProviderId,
            ServiceId = invoice.ServiceId,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            BasePrice = basePrice,
            LocationAdjustmentAmount = locationAdjustment,
            DiscountAmount = invoice.TotalDiscountAmount > 0 ? invoice.TotalDiscountAmount : invoice.DiscountAmount,
            CgstAmount = cgstAmount,
            SgstAmount = sgstAmount,
            IgstAmount = igstAmount,
            ServiceChargeAmount = serviceCharge,
            PlatformChargeAmount = platformCharge,
            TotalTaxAmount = invoice.TotalTaxAmount,
            Subtotal = invoice.Subtotal,
            TotalAmount = invoice.TotalAmount,
            PaymentStatus = invoice.PaymentStatus,
            PdfPath = invoice.PdfPath,
            Notes = invoice.Notes,
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt,
            Service = invoice.Booking?.Service != null ? new InvoiceServiceDto
            {
                Id = invoice.Booking.Service.Id,
                ServiceName = invoice.Booking.Service.ServiceName,
                Description = invoice.Booking.Service.Description
            } : null,
            Customer = invoice.Booking?.Customer != null ? new InvoiceCustomerDto
            {
                Id = invoice.Booking.Customer.Id,
                Name = invoice.Booking.Customer.Name,
                Email = invoice.Booking.Customer.Email,
                MobileNumber = invoice.Booking.Customer.MobileNumber
            } : null,
            ServiceProvider = invoice.Booking?.ServiceProvider != null ? new InvoiceServiceProviderDto
            {
                Id = invoice.Booking.ServiceProvider.Id,
                Name = invoice.Booking.ServiceProvider.Name,
                Email = invoice.Booking.ServiceProvider.Email,
                MobileNumber = invoice.Booking.ServiceProvider.MobileNumber
            } : null,
            Company = companyConfig != null ? new InvoiceCompanyDto
            {
                CompanyName = companyConfig.CompanyName,
                CompanyAddressLine1 = companyConfig.CompanyAddressLine1,
                CompanyAddressLine2 = companyConfig.CompanyAddressLine2,
                CompanyCity = companyConfig.CompanyCity,
                CompanyPincode = companyConfig.CompanyPincode,
                CompanyState = companyConfig.CompanyState?.Name,
                CompanyPhone = companyConfig.CompanyPhone,
                CompanyEmail = companyConfig.CompanyEmail,
                CompanyGstin = companyConfig.CompanyGstin,
                CompanyPan = companyConfig.CompanyPan,
                CompanyWebsite = companyConfig.CompanyWebsite,
                InvoiceFooterText = companyConfig.InvoiceFooterText
            } : null
        };
    }

    public async Task<ServiceResult> ListInvoicesAsync(int pageNumber = 1, int pageSize = 10, string? paymentStatus = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.InvoiceMasters
                .Include(i => i.Booking)
                    .ThenInclude(b => b!.Customer)
                .Include(i => i.Booking)
                    .ThenInclude(b => b!.ServiceProvider)
                .Include(i => i.Booking)
                    .ThenInclude(b => b!.Service)
                .AsQueryable();

            // Filter by payment status if provided
            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                query = query.Where(i => i.PaymentStatus == paymentStatus);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var invoices = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            // Map to response DTOs
            var invoiceResponses = new List<InvoiceResponse>();
            foreach (var invoice in invoices)
            {
                var response = await MapToInvoiceResponseAsync(invoice, cancellationToken);
                invoiceResponses.Add(response);
            }

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var listResponse = new InvoiceListResponse
            {
                Items = invoiceResponses,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages
            };

            return ServiceResult.Ok(listResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing invoices");
            return ServiceResult.BadRequest($"An error occurred while listing invoices: {ex.Message}");
        }
    }
}
