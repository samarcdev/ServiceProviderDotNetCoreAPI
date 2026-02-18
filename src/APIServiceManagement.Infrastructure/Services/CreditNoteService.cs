using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using APIServiceManagement.Application.Interfaces.Services;
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

public class CreditNoteService : ICreditNoteService
{
    private readonly AppDbContext _context;
    private readonly IPdfGenerationService _pdfGenerationService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<CreditNoteService> _logger;

    public CreditNoteService(
        AppDbContext context,
        IPdfGenerationService pdfGenerationService,
        IFileStorageService fileStorageService,
        ILogger<CreditNoteService> logger)
    {
        _context = context;
        _pdfGenerationService = pdfGenerationService;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public async Task<ServiceResult> CreateCreditNoteAsync(int invoiceId, CreateCreditNoteRequest request, Guid createdBy, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            try
            {
                // Load invoice with all related entities
                var invoice = await _context.InvoiceMasters
                    .Include(i => i.Booking)
                    .Include(i => i.Customer)
                    .Include(i => i.ServiceProvider)
                    .Include(i => i.Service)
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

                // Validate credit type
                if (request.CreditType != "Full" && request.CreditType != "Partial")
                {
                    return ServiceResult.BadRequest("Credit type must be 'Full' or 'Partial'.");
                }

                // Validate credit reason
                if (string.IsNullOrWhiteSpace(request.CreditReason))
                {
                    return ServiceResult.BadRequest("Credit reason is required.");
                }

                // Validate total amount
                if (request.TotalAmount <= 0)
                {
                    return ServiceResult.BadRequest("Total credit amount must be greater than zero.");
                }

                // For Full credit, validate amount matches invoice total
                if (request.CreditType == "Full")
                {
                    if (request.TotalAmount != invoice.TotalAmount)
                    {
                        _logger.LogWarning("Full credit amount {CreditAmount} does not match invoice total {InvoiceTotal}. Using invoice total.", 
                            request.TotalAmount, invoice.TotalAmount);
                        // Use invoice total for full credit
                    }
                }

                // Check if credit note already exists for this invoice (idempotency check)
                var existingCreditNote = await _context.CreditNoteMasters
                    .AsNoTracking()
                    .Where(cn => cn.InvoiceId == invoiceId && cn.Status != "Cancelled")
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingCreditNote != null)
                {
                    _logger.LogWarning("Active credit note already exists for invoice {InvoiceId}. Credit Note: {CreditNoteNumber}", 
                        invoiceId, existingCreditNote.CreditNoteNumber);
                    return ServiceResult.BadRequest(new MessageResponse 
                    { 
                        Message = $"Active credit note already exists for this invoice. Credit Note Number: {existingCreditNote.CreditNoteNumber}" 
                    });
                }

                // Generate credit note number
                var creditNoteNumber = await GenerateCreditNoteNumberAsync(cancellationToken);

                // Calculate amounts (use provided values or calculate from invoice)
                var subtotal = request.Subtotal ?? invoice.Subtotal;
                var totalTaxAmount = request.TotalTaxAmount ?? 0;
                var totalDiscountAmount = request.TotalDiscountAmount ?? 0;
                var totalAddonAmount = request.TotalAddonAmount ?? 0;
                var totalAmount = request.TotalAmount;

                // Create credit note master
                var creditNote = new CreditNoteMaster
                {
                    CreditNoteNumber = creditNoteNumber,
                    InvoiceId = invoiceId,
                    BookingId = invoice.BookingId,
                    CustomerId = invoice.CustomerId,
                    ServiceProviderId = invoice.ServiceProviderId,
                    ServiceId = invoice.ServiceId,
                    CreditType = request.CreditType,
                    CreditReason = request.CreditReason,
                    CreditNoteDate = DateTime.UtcNow,
                    Subtotal = subtotal,
                    TotalTaxAmount = totalTaxAmount,
                    TotalDiscountAmount = totalDiscountAmount,
                    TotalAddonAmount = totalAddonAmount,
                    TotalAmount = totalAmount,
                    Status = "Issued",
                    Notes = request.Notes,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.CreditNoteMasters.Add(creditNote);
                await _context.SaveChangesAsync(cancellationToken);

                // Add taxes if provided
                if (request.Taxes != null && request.Taxes.Any())
                {
                    foreach (var taxDto in request.Taxes)
                    {
                        var tax = await _context.TaxMasters
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.TaxId == taxDto.TaxId && t.IsActive, cancellationToken);

                        if (tax != null)
                        {
                            creditNote.CreditNoteTaxes.Add(new CreditNoteTax
                            {
                                CreditNoteId = creditNote.CreditNoteId,
                                TaxId = tax.TaxId,
                                TaxName = tax.TaxName,
                                TaxPercentage = tax.TaxPercentage,
                                TaxableAmount = taxDto.TaxableAmount,
                                TaxAmount = taxDto.TaxAmount,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
                else if (request.CreditType == "Full" && invoice.InvoiceTaxes.Any())
                {
                    // For full credit, copy all taxes from invoice
                    foreach (var invoiceTax in invoice.InvoiceTaxes)
                    {
                        creditNote.CreditNoteTaxes.Add(new CreditNoteTax
                        {
                            CreditNoteId = creditNote.CreditNoteId,
                            TaxId = invoiceTax.TaxId,
                            TaxName = invoiceTax.TaxName,
                            TaxPercentage = invoiceTax.TaxPercentage,
                            TaxableAmount = invoiceTax.TaxableAmount,
                            TaxAmount = invoiceTax.TaxAmount,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    totalTaxAmount = invoice.TotalTaxAmount;
                }

                // Add discounts if provided
                if (request.Discounts != null && request.Discounts.Any())
                {
                    foreach (var discountDto in request.Discounts)
                    {
                        DiscountMaster? discount = null;
                        if (discountDto.DiscountId.HasValue)
                        {
                            discount = await _context.DiscountMasters
                                .AsNoTracking()
                                .FirstOrDefaultAsync(d => d.DiscountId == discountDto.DiscountId.Value, cancellationToken);
                        }

                        creditNote.CreditNoteDiscounts.Add(new CreditNoteDiscount
                        {
                            CreditNoteId = creditNote.CreditNoteId,
                            DiscountId = discountDto.DiscountId,
                            DiscountName = discount?.DiscountName,
                            DiscountType = discount?.DiscountType,
                            DiscountValue = discount?.DiscountValue,
                            DiscountAmount = discountDto.DiscountAmount,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                else if (request.CreditType == "Full" && invoice.InvoiceDiscounts.Any())
                {
                    // For full credit, copy all discounts from invoice
                    foreach (var invoiceDiscount in invoice.InvoiceDiscounts)
                    {
                        creditNote.CreditNoteDiscounts.Add(new CreditNoteDiscount
                        {
                            CreditNoteId = creditNote.CreditNoteId,
                            DiscountId = invoiceDiscount.DiscountId,
                            DiscountName = invoiceDiscount.DiscountName,
                            DiscountType = invoiceDiscount.DiscountType,
                            DiscountValue = invoiceDiscount.DiscountValue,
                            DiscountAmount = invoiceDiscount.DiscountAmount,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    totalDiscountAmount = invoice.TotalDiscountAmount;
                }

                // Add add-ons if provided
                if (request.AddOns != null && request.AddOns.Any())
                {
                    foreach (var addOnDto in request.AddOns)
                    {
                        creditNote.CreditNoteAddOns.Add(new CreditNoteAddOn
                        {
                            CreditNoteId = creditNote.CreditNoteId,
                            AddonName = addOnDto.AddonName,
                            AddonDescription = addOnDto.AddonDescription,
                            Quantity = addOnDto.Quantity,
                            UnitPrice = addOnDto.UnitPrice,
                            TotalPrice = addOnDto.TotalPrice,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                else if (request.CreditType == "Full" && invoice.InvoiceAddOns.Any())
                {
                    // For full credit, copy all add-ons from invoice
                    foreach (var invoiceAddOn in invoice.InvoiceAddOns)
                    {
                        creditNote.CreditNoteAddOns.Add(new CreditNoteAddOn
                        {
                            CreditNoteId = creditNote.CreditNoteId,
                            AddonName = invoiceAddOn.AddonName ?? "Add-on",
                            AddonDescription = invoiceAddOn.AddonDescription,
                            Quantity = invoiceAddOn.Quantity ?? 1,
                            UnitPrice = invoiceAddOn.UnitPrice ?? invoiceAddOn.Price,
                            TotalPrice = invoiceAddOn.TotalPrice ?? invoiceAddOn.TotalAmount,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    totalAddonAmount = invoice.TotalAddonAmount;
                }

                // Update totals if they were recalculated
                if (request.CreditType == "Full")
                {
                    creditNote.TotalTaxAmount = totalTaxAmount;
                    creditNote.TotalDiscountAmount = totalDiscountAmount;
                    creditNote.TotalAddonAmount = totalAddonAmount;
                    creditNote.TotalAmount = invoice.TotalAmount;
                }

                // Create audit history entry
                creditNote.AuditHistory.Add(new CreditNoteAuditHistory
                {
                    CreditNoteId = creditNote.CreditNoteId,
                    Action = "Created",
                    NewStatus = "Issued",
                    ChangedBy = createdBy,
                    ChangeDescription = $"Credit note created for invoice {invoice.InvoiceNumber}. Reason: {request.CreditReason}",
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync(cancellationToken);

                // Generate PDF
                try
                {
                    var creditNoteResponse = await MapToCreditNoteResponseAsync(creditNote, cancellationToken);
                    var pdfBytes = await _pdfGenerationService.GenerateCreditNotePdfAsync(creditNoteResponse, cancellationToken);
                    
                    var pdfFileName = $"creditnote_{creditNoteNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                    var pdfPath = await _fileStorageService.SaveFileAsync(pdfBytes, pdfFileName, "creditnotes", cancellationToken);
                    
                    creditNote.PdfPath = pdfPath;
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception pdfEx)
                {
                    _logger.LogError(pdfEx, "Error generating PDF for credit note {CreditNoteNumber}. Credit note will be saved without PDF.", creditNoteNumber);
                    // Continue without PDF - credit note is still valid
                }

                _logger.LogInformation("Credit note {CreditNoteNumber} created successfully for invoice {InvoiceId}. Total Amount: {TotalAmount}", 
                    creditNoteNumber, invoiceId, creditNote.TotalAmount);

                var response = await MapToCreditNoteResponseAsync(creditNote, cancellationToken);
                return ServiceResult.Created(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating credit note for invoice {InvoiceId}", invoiceId);
                return ServiceResult.BadRequest($"Error creating credit note: {ex.Message}");
            }
        });
    }

    public async Task<ServiceResult> GetCreditNoteByIdAsync(int creditNoteId, CancellationToken cancellationToken = default)
    {
        try
        {
            var creditNote = await _context.CreditNoteMasters
                .AsNoTracking()
                .Include(cn => cn.Invoice)
                .Include(cn => cn.Booking)
                .Include(cn => cn.Customer)
                .Include(cn => cn.ServiceProvider)
                .Include(cn => cn.Service)
                .Include(cn => cn.CreatedByUser)
                .Include(cn => cn.CreditNoteTaxes)
                    .ThenInclude(t => t.Tax)
                .Include(cn => cn.CreditNoteDiscounts)
                    .ThenInclude(d => d.Discount)
                .Include(cn => cn.CreditNoteAddOns)
                .Include(cn => cn.Applications)
                    .ThenInclude(a => a.AppliedByUser)
                .FirstOrDefaultAsync(cn => cn.CreditNoteId == creditNoteId, cancellationToken);

            if (creditNote == null)
            {
                return ServiceResult.NotFound(new MessageResponse { Message = "Credit note not found." });
            }

            var response = await MapToCreditNoteResponseAsync(creditNote, cancellationToken);
            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credit note {CreditNoteId}", creditNoteId);
            return ServiceResult.BadRequest($"Error retrieving credit note: {ex.Message}");
        }
    }

    public async Task<ServiceResult> GetCreditNoteByNumberAsync(string creditNoteNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var creditNote = await _context.CreditNoteMasters
                .AsNoTracking()
                .Include(cn => cn.Invoice)
                .Include(cn => cn.Booking)
                .Include(cn => cn.Customer)
                .Include(cn => cn.ServiceProvider)
                .Include(cn => cn.Service)
                .Include(cn => cn.CreatedByUser)
                .Include(cn => cn.CreditNoteTaxes)
                    .ThenInclude(t => t.Tax)
                .Include(cn => cn.CreditNoteDiscounts)
                    .ThenInclude(d => d.Discount)
                .Include(cn => cn.CreditNoteAddOns)
                .Include(cn => cn.Applications)
                    .ThenInclude(a => a.AppliedByUser)
                .FirstOrDefaultAsync(cn => cn.CreditNoteNumber == creditNoteNumber, cancellationToken);

            if (creditNote == null)
            {
                return ServiceResult.NotFound(new MessageResponse { Message = "Credit note not found." });
            }

            var response = await MapToCreditNoteResponseAsync(creditNote, cancellationToken);
            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credit note {CreditNoteNumber}", creditNoteNumber);
            return ServiceResult.BadRequest($"Error retrieving credit note: {ex.Message}");
        }
    }

    public async Task<ServiceResult> GetCreditNotesByInvoiceIdAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var creditNotes = await _context.CreditNoteMasters
                .AsNoTracking()
                .Include(cn => cn.Invoice)
                .Include(cn => cn.Customer)
                .Include(cn => cn.Service)
                .Include(cn => cn.CreatedByUser)
                .Where(cn => cn.InvoiceId == invoiceId)
                .OrderByDescending(cn => cn.CreatedAt)
                .ToListAsync(cancellationToken);

            var responses = await Task.WhenAll(creditNotes.Select(cn => MapToCreditNoteResponseAsync(cn, cancellationToken)));
            
            return ServiceResult.Ok(responses.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credit notes for invoice {InvoiceId}", invoiceId);
            return ServiceResult.BadRequest($"Error retrieving credit notes: {ex.Message}");
        }
    }

    public async Task<ServiceResult> ListCreditNotesAsync(int? pageNumber, int? pageSize, string? status, DateTime? startDate, DateTime? endDate, Guid? customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.CreditNoteMasters
                .AsNoTracking()
                .Include(cn => cn.Invoice)
                .Include(cn => cn.Customer)
                .Include(cn => cn.Service)
                .Include(cn => cn.CreatedByUser)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(cn => cn.Status == status);
            }

            if (startDate.HasValue)
            {
                query = query.Where(cn => cn.CreditNoteDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(cn => cn.CreditNoteDate <= endDate.Value);
            }

            if (customerId.HasValue)
            {
                query = query.Where(cn => cn.CustomerId == customerId.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var page = pageNumber ?? 1;
            var size = pageSize ?? 50;
            var skip = (page - 1) * size;

            var creditNotes = await query
                .OrderByDescending(cn => cn.CreatedAt)
                .Skip(skip)
                .Take(size)
                .ToListAsync(cancellationToken);

            var responses = await Task.WhenAll(creditNotes.Select(cn => MapToCreditNoteResponseAsync(cn, cancellationToken)));

            var response = new CreditNoteListResponse
            {
                CreditNotes = responses.ToList(),
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = size,
                TotalPages = (int)Math.Ceiling(totalCount / (double)size)
            };

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing credit notes");
            return ServiceResult.BadRequest($"Error listing credit notes: {ex.Message}");
        }
    }

    public async Task<ServiceResult> ApplyCreditNoteAsync(int creditNoteId, ApplyCreditNoteRequest request, Guid appliedBy, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            try
            {
                var creditNote = await _context.CreditNoteMasters
                    .Include(cn => cn.Invoice)
                    .FirstOrDefaultAsync(cn => cn.CreditNoteId == creditNoteId, cancellationToken);

                if (creditNote == null)
                {
                    return ServiceResult.NotFound(new MessageResponse { Message = "Credit note not found." });
                }

                if (creditNote.Status != "Issued")
                {
                    return ServiceResult.BadRequest($"Credit note cannot be applied. Current status: {creditNote.Status}");
                }

                if (request.AppliedAmount <= 0 || request.AppliedAmount > creditNote.TotalAmount)
                {
                    return ServiceResult.BadRequest("Applied amount must be greater than zero and not exceed credit note total amount.");
                }

                var oldStatus = creditNote.Status;
                creditNote.Status = "Applied";
                creditNote.UpdatedAt = DateTime.UtcNow;

                // Create application record
                var application = new CreditNoteApplication
                {
                    CreditNoteId = creditNoteId,
                    InvoiceId = creditNote.InvoiceId,
                    AppliedAmount = request.AppliedAmount,
                    ApplicationDate = DateTime.UtcNow,
                    BankAccountNumber = request.BankAccountNumber,
                    BankName = request.BankName,
                    TransactionReference = request.TransactionReference,
                    AppliedBy = appliedBy,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.CreditNoteApplications.Add(application);

                // Create audit history entry
                creditNote.AuditHistory.Add(new CreditNoteAuditHistory
                {
                    CreditNoteId = creditNoteId,
                    Action = "Applied",
                    OldStatus = oldStatus,
                    NewStatus = "Applied",
                    ChangedBy = appliedBy,
                    ChangeDescription = $"Credit note applied. Bank transfer details: {request.BankName} - {request.TransactionReference}",
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Credit note {CreditNoteNumber} applied successfully. Applied amount: {AppliedAmount}", 
                    creditNote.CreditNoteNumber, request.AppliedAmount);

                var response = await MapToCreditNoteResponseAsync(creditNote, cancellationToken);
                return ServiceResult.Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying credit note {CreditNoteId}", creditNoteId);
                return ServiceResult.BadRequest($"Error applying credit note: {ex.Message}");
            }
        });
    }

    public async Task<ServiceResult> CancelCreditNoteAsync(int creditNoteId, string? reason, Guid cancelledBy, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            try
            {
                var creditNote = await _context.CreditNoteMasters
                    .FirstOrDefaultAsync(cn => cn.CreditNoteId == creditNoteId, cancellationToken);

                if (creditNote == null)
                {
                    return ServiceResult.NotFound(new MessageResponse { Message = "Credit note not found." });
                }

                if (creditNote.Status == "Applied")
                {
                    return ServiceResult.BadRequest("Cannot cancel an applied credit note.");
                }

                if (creditNote.Status == "Cancelled")
                {
                    return ServiceResult.BadRequest("Credit note is already cancelled.");
                }

                var oldStatus = creditNote.Status;
                creditNote.Status = "Cancelled";
                creditNote.UpdatedAt = DateTime.UtcNow;

                // Create audit history entry
                creditNote.AuditHistory.Add(new CreditNoteAuditHistory
                {
                    CreditNoteId = creditNoteId,
                    Action = "Cancelled",
                    OldStatus = oldStatus,
                    NewStatus = "Cancelled",
                    ChangedBy = cancelledBy,
                    ChangeDescription = reason ?? "Credit note cancelled",
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Credit note {CreditNoteNumber} cancelled successfully.", creditNote.CreditNoteNumber);

                var response = await MapToCreditNoteResponseAsync(creditNote, cancellationToken);
                return ServiceResult.Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling credit note {CreditNoteId}", creditNoteId);
                return ServiceResult.BadRequest($"Error cancelling credit note: {ex.Message}");
            }
        });
    }

    public async Task<ServiceResult> GetCreditNotePdfAsync(int creditNoteId, CancellationToken cancellationToken = default)
    {
        try
        {
            var creditNote = await _context.CreditNoteMasters
                .Include(cn => cn.Invoice)
                .Include(cn => cn.Booking)
                    .ThenInclude(b => b.Service)
                .Include(cn => cn.Booking)
                    .ThenInclude(b => b.Customer)
                .Include(cn => cn.Booking)
                    .ThenInclude(b => b.ServiceProvider)
                .Include(cn => cn.CreditNoteTaxes)
                    .ThenInclude(t => t.Tax)
                .Include(cn => cn.CreditNoteDiscounts)
                    .ThenInclude(d => d.Discount)
                .Include(cn => cn.CreditNoteAddOns)
                .FirstOrDefaultAsync(cn => cn.CreditNoteId == creditNoteId, cancellationToken);

            if (creditNote == null)
            {
                return ServiceResult.NotFound(new MessageResponse { Message = "Credit note not found." });
            }

            byte[] pdfBytes;

            // If PDF exists in storage, retrieve it
            if (!string.IsNullOrEmpty(creditNote.PdfPath))
            {
                pdfBytes = await _fileStorageService.GetFileAsync(creditNote.PdfPath, cancellationToken);
            }
            else
            {
                // Generate PDF on the fly
                var creditNoteResponse = await MapToCreditNoteResponseAsync(creditNote, cancellationToken);
                pdfBytes = await _pdfGenerationService.GenerateCreditNotePdfAsync(creditNoteResponse, cancellationToken);
            }

            var response = new CreditNotePdfResponse
            {
                PdfBytes = pdfBytes,
                CreditNoteNumber = creditNote.CreditNoteNumber,
                FileName = $"creditnote_{creditNote.CreditNoteNumber}.pdf",
                ContentType = "application/pdf"
            };

            return ServiceResult.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PDF for credit note {CreditNoteId}", creditNoteId);
            return ServiceResult.BadRequest($"Error retrieving PDF: {ex.Message}");
        }
    }

    public async Task<ServiceResult> GetCreditNoteSummaryReportAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.CreditNoteMasters.AsNoTracking().AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(cn => cn.CreditNoteDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(cn => cn.CreditNoteDate <= endDate.Value);
            }

            var creditNotes = await query.ToListAsync(cancellationToken);

            var report = new CreditNoteSummaryReportResponse
            {
                TotalCreditsIssued = creditNotes.Sum(cn => cn.TotalAmount),
                TotalCreditsApplied = creditNotes.Where(cn => cn.Status == "Applied").Sum(cn => cn.TotalAmount),
                TotalCreditsCancelled = creditNotes.Where(cn => cn.Status == "Cancelled").Sum(cn => cn.TotalAmount),
                TotalCount = creditNotes.Count,
                IssuedCount = creditNotes.Count(cn => cn.Status == "Issued"),
                AppliedCount = creditNotes.Count(cn => cn.Status == "Applied"),
                CancelledCount = creditNotes.Count(cn => cn.Status == "Cancelled"),
                ReportStartDate = startDate,
                ReportEndDate = endDate
            };

            return ServiceResult.Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating credit note summary report");
            return ServiceResult.BadRequest($"Error generating report: {ex.Message}");
        }
    }

    public async Task<ServiceResult> GetCreditNotesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var creditNotes = await _context.CreditNoteMasters
                .AsNoTracking()
                .Include(cn => cn.Invoice)
                .Include(cn => cn.Customer)
                .Include(cn => cn.Service)
                .Where(cn => cn.CreditNoteDate >= startDate && cn.CreditNoteDate <= endDate)
                .OrderByDescending(cn => cn.CreditNoteDate)
                .ToListAsync(cancellationToken);

            var responses = await Task.WhenAll(creditNotes.Select(cn => MapToCreditNoteResponseAsync(cn, cancellationToken)));

            return ServiceResult.Ok(responses.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credit notes by date range");
            return ServiceResult.BadRequest($"Error retrieving credit notes: {ex.Message}");
        }
    }

    public async Task<ServiceResult> GetCreditNotesByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var creditNotes = await _context.CreditNoteMasters
                .AsNoTracking()
                .Include(cn => cn.Invoice)
                .Include(cn => cn.Customer)
                .Include(cn => cn.Service)
                .Where(cn => cn.CustomerId == customerId)
                .OrderByDescending(cn => cn.CreatedAt)
                .ToListAsync(cancellationToken);

            var responses = await Task.WhenAll(creditNotes.Select(cn => MapToCreditNoteResponseAsync(cn, cancellationToken)));

            return ServiceResult.Ok(responses.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credit notes for customer {CustomerId}", customerId);
            return ServiceResult.BadRequest($"Error retrieving credit notes: {ex.Message}");
        }
    }

    private async Task<string> GenerateCreditNoteNumberAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow;
        var prefix = $"CN-{today:yyyyMMdd}-";
        
        var lastCreditNote = await _context.CreditNoteMasters
            .AsNoTracking()
            .Where(cn => cn.CreditNoteNumber.StartsWith(prefix))
            .OrderByDescending(cn => cn.CreditNoteNumber)
            .FirstOrDefaultAsync(cancellationToken);

        int sequence = 1;
        if (lastCreditNote != null)
        {
            var lastSequence = lastCreditNote.CreditNoteNumber.Replace(prefix, "");
            if (int.TryParse(lastSequence, out var lastSeq))
            {
                sequence = lastSeq + 1;
            }
        }

        string creditNoteNumber;
        int maxAttempts = 100;
        int attempts = 0;
        do
        {
            creditNoteNumber = $"{prefix}{sequence:D4}";
            var exists = await _context.CreditNoteMasters
                .AsNoTracking()
                .AnyAsync(cn => cn.CreditNoteNumber == creditNoteNumber, cancellationToken);
            
            if (!exists)
                break;
                
            sequence++;
            attempts++;
        } while (attempts < maxAttempts);

        if (attempts >= maxAttempts)
        {
            throw new InvalidOperationException("Unable to generate unique credit note number after multiple attempts.");
        }

        return creditNoteNumber;
    }

    private async Task<CreditNoteResponse> MapToCreditNoteResponseAsync(CreditNoteMaster creditNote, CancellationToken cancellationToken = default)
    {
        // Ensure related entities are loaded
        if (creditNote.Invoice == null)
        {
            creditNote.Invoice = await _context.InvoiceMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvoiceId == creditNote.InvoiceId, cancellationToken);
        }

        if (creditNote.Customer == null)
        {
            creditNote.Customer = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == creditNote.CustomerId, cancellationToken);
        }

        if (creditNote.Service == null)
        {
            creditNote.Service = await _context.Services
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == creditNote.ServiceId, cancellationToken);
        }

        if (creditNote.CreatedByUser == null)
        {
            creditNote.CreatedByUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == creditNote.CreatedBy, cancellationToken);
        }

        // Get company configuration
        var companyConfig = await _context.CompanyConfigurations
            .AsNoTracking()
            .Include(c => c.CompanyState)
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var response = new CreditNoteResponse
        {
            Id = creditNote.CreditNoteId,
            CreditNoteNumber = creditNote.CreditNoteNumber,
            InvoiceId = creditNote.InvoiceId,
            InvoiceNumber = creditNote.Invoice?.InvoiceNumber,
            BookingId = creditNote.BookingId,
            CustomerId = creditNote.CustomerId,
            ServiceProviderId = creditNote.ServiceProviderId,
            ServiceId = creditNote.ServiceId,
            CreditType = creditNote.CreditType,
            CreditReason = creditNote.CreditReason,
            CreditNoteDate = creditNote.CreditNoteDate,
            Subtotal = creditNote.Subtotal,
            TotalTaxAmount = creditNote.TotalTaxAmount,
            TotalDiscountAmount = creditNote.TotalDiscountAmount,
            TotalAddonAmount = creditNote.TotalAddonAmount,
            TotalAmount = creditNote.TotalAmount,
            Status = creditNote.Status,
            PdfPath = creditNote.PdfPath,
            Notes = creditNote.Notes,
            CreatedBy = creditNote.CreatedBy,
            CreatedByName = creditNote.CreatedByUser?.Name,
            CreatedAt = creditNote.CreatedAt,
            UpdatedAt = creditNote.UpdatedAt,
            Service = creditNote.Service != null ? new CreditNoteServiceDto
            {
                Id = creditNote.Service.Id,
                ServiceName = creditNote.Service.ServiceName,
                Description = creditNote.Service.Description
            } : null,
            Customer = creditNote.Customer != null ? new CreditNoteCustomerDto
            {
                Id = creditNote.Customer.Id,
                Name = creditNote.Customer.Name,
                Email = creditNote.Customer.Email,
                MobileNumber = creditNote.Customer.MobileNumber
            } : null,
            ServiceProvider = creditNote.ServiceProvider != null ? new CreditNoteServiceProviderDto
            {
                Id = creditNote.ServiceProvider.Id,
                Name = creditNote.ServiceProvider.Name,
                Email = creditNote.ServiceProvider.Email,
                MobileNumber = creditNote.ServiceProvider.MobileNumber
            } : null,
            Company = companyConfig != null ? new CreditNoteCompanyDto
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
            } : null,
            Taxes = creditNote.CreditNoteTaxes.Select(t => new CreditNoteTaxResponseDto
            {
                Id = t.CreditNoteTaxId,
                TaxId = t.TaxId,
                TaxName = t.TaxName,
                TaxPercentage = t.TaxPercentage,
                TaxableAmount = t.TaxableAmount,
                TaxAmount = t.TaxAmount
            }).ToList(),
            Discounts = creditNote.CreditNoteDiscounts.Select(d => new CreditNoteDiscountResponseDto
            {
                Id = d.CreditNoteDiscountId,
                DiscountId = d.DiscountId,
                DiscountName = d.DiscountName,
                DiscountType = d.DiscountType,
                DiscountValue = d.DiscountValue,
                DiscountAmount = d.DiscountAmount
            }).ToList(),
            AddOns = creditNote.CreditNoteAddOns.Select(a => new CreditNoteAddOnResponseDto
            {
                Id = a.CreditNoteAddonId,
                AddonName = a.AddonName,
                AddonDescription = a.AddonDescription,
                Quantity = a.Quantity,
                UnitPrice = a.UnitPrice,
                TotalPrice = a.TotalPrice
            }).ToList(),
            Applications = creditNote.Applications.Select(a => new CreditNoteApplicationResponseDto
            {
                Id = a.ApplicationId,
                AppliedAmount = a.AppliedAmount,
                ApplicationDate = a.ApplicationDate,
                BankAccountNumber = a.BankAccountNumber,
                BankName = a.BankName,
                TransactionReference = a.TransactionReference,
                AppliedBy = a.AppliedBy,
                AppliedByName = a.AppliedByUser?.Name,
                Notes = a.Notes,
                CreatedAt = a.CreatedAt
            }).ToList()
        };

        return response;
    }
}
