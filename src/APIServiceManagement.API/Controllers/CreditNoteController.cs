using APIServiceManagement.API.Attributes;
using APIServiceManagement.API.Extensions;
using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using APIServiceManagement.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CreditNoteController : ControllerBase
{
    private readonly ICreditNoteService _creditNoteService;

    public CreditNoteController(ICreditNoteService creditNoteService)
    {
        _creditNoteService = creditNoteService;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException("User ID not found in token"));
    }

    /// <summary>
    /// Create credit note for an invoice (Admin only)
    /// </summary>
    [HttpPost("create")]
    [AuthorizeAdmin]
    public async Task<IActionResult> CreateCreditNote(
        [FromBody] CreateCreditNoteRequest request,
        CancellationToken cancellationToken)
    {
        var createdBy = GetUserId();
        var result = await _creditNoteService.CreateCreditNoteAsync(request.InvoiceId, request, createdBy, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get credit note by ID (Admin only)
    /// </summary>
    [HttpGet("{creditNoteId:int}")]
    [AuthorizeAdmin]
    public async Task<IActionResult> GetCreditNoteById(
        int creditNoteId,
        CancellationToken cancellationToken)
    {
        var result = await _creditNoteService.GetCreditNoteByIdAsync(creditNoteId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get credit note by credit note number (Admin only)
    /// </summary>
    [HttpGet("number/{creditNoteNumber}")]
    [AuthorizeAdmin]
    public async Task<IActionResult> GetCreditNoteByNumber(
        string creditNoteNumber,
        CancellationToken cancellationToken)
    {
        var result = await _creditNoteService.GetCreditNoteByNumberAsync(creditNoteNumber, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get all credit notes for an invoice (Admin only)
    /// </summary>
    [HttpGet("invoice/{invoiceId:int}")]
    [AuthorizeAdmin]
    public async Task<IActionResult> GetCreditNotesByInvoiceId(
        int invoiceId,
        CancellationToken cancellationToken)
    {
        var result = await _creditNoteService.GetCreditNotesByInvoiceIdAsync(invoiceId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// List all credit notes with filters (Admin only)
    /// GET api/CreditNote?pageNumber=1&amp;pageSize=10
    /// </summary>
    [HttpGet]
    [AuthorizeAdmin]
    public async Task<IActionResult> ListCreditNotes(
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        [FromQuery] string? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? customerId,
        CancellationToken cancellationToken)
    {
        var result = await _creditNoteService.ListCreditNotesAsync(
            pageNumber, 
            pageSize, 
            status, 
            startDate, 
            endDate, 
            customerId, 
            cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Apply credit note (mark as applied with bank transfer details) (Admin only)
    /// </summary>
    [HttpPut("{creditNoteId:int}/apply")]
    [AuthorizeAdmin]
    public async Task<IActionResult> ApplyCreditNote(
        int creditNoteId,
        [FromBody] ApplyCreditNoteRequest request,
        CancellationToken cancellationToken)
    {
        var appliedBy = GetUserId();
        var result = await _creditNoteService.ApplyCreditNoteAsync(creditNoteId, request, appliedBy, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Cancel credit note (Admin only)
    /// </summary>
    [HttpPut("{creditNoteId:int}/cancel")]
    [AuthorizeAdmin]
    public async Task<IActionResult> CancelCreditNote(
        int creditNoteId,
        [FromBody] CancelCreditNoteRequest? request,
        CancellationToken cancellationToken)
    {
        var cancelledBy = GetUserId();
        var result = await _creditNoteService.CancelCreditNoteAsync(creditNoteId, request?.Reason, cancelledBy, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Download credit note PDF by ID (Admin only)
    /// </summary>
    [HttpGet("{creditNoteId:int}/pdf")]
    [AuthorizeAdmin]
    public async Task<IActionResult> GetCreditNotePdf(
        int creditNoteId,
        CancellationToken cancellationToken)
    {
        var result = await _creditNoteService.GetCreditNotePdfAsync(creditNoteId, cancellationToken);
        
        if (result.StatusCode != System.Net.HttpStatusCode.OK || result.Payload is not CreditNotePdfResponse pdfResponse)
        {
            return result.ToActionResult();
        }

        return File(pdfResponse.PdfBytes, pdfResponse.ContentType, pdfResponse.FileName);
    }

    /// <summary>
    /// Get credit note summary report (Admin only)
    /// </summary>
    [HttpGet("reports/summary")]
    [AuthorizeAdmin]
    public async Task<IActionResult> GetCreditNoteSummaryReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var result = await _creditNoteService.GetCreditNoteSummaryReportAsync(startDate, endDate, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get credit notes by date range (Admin only)
    /// </summary>
    [HttpGet("reports/by-date")]
    [AuthorizeAdmin]
    public async Task<IActionResult> GetCreditNotesByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        var result = await _creditNoteService.GetCreditNotesByDateRangeAsync(startDate, endDate, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get credit notes by customer (Admin only)
    /// </summary>
    [HttpGet("reports/by-customer/{customerId:guid}")]
    [AuthorizeAdmin]
    public async Task<IActionResult> GetCreditNotesByCustomer(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var result = await _creditNoteService.GetCreditNotesByCustomerAsync(customerId, cancellationToken);
        return result.ToActionResult();
    }
}

public class CancelCreditNoteRequest
{
    public string? Reason { get; set; }
}
