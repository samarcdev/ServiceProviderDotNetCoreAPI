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
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoiceController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    /// <summary>
    /// Generate invoice for a completed booking
    /// </summary>
    [HttpPost("generate")]
    [Authorize(Roles = "ServiceProvider,Admin,MasterAdmin,DefaultAdmin")]
    public async Task<IActionResult> GenerateInvoice(
        [FromBody] GenerateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _invoiceService.GenerateInvoiceAsync(request.BookingId, request, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get invoice by ID
    /// </summary>
    [HttpGet("{invoiceId:int}")]
    public async Task<IActionResult> GetInvoiceById(
        int invoiceId,
        CancellationToken cancellationToken)
    {
        var result = await _invoiceService.GetInvoiceByIdAsync(invoiceId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get invoice by invoice number
    /// </summary>
    [HttpGet("number/{invoiceNumber}")]
    public async Task<IActionResult> GetInvoiceByInvoiceNumber(
        string invoiceNumber,
        CancellationToken cancellationToken)
    {
        var result = await _invoiceService.GetInvoiceByInvoiceNumberAsync(invoiceNumber, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get invoice by booking ID
    /// </summary>
    [HttpGet("booking/{bookingId:guid}")]
    public async Task<IActionResult> GetInvoiceByBookingId(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        var result = await _invoiceService.GetInvoiceByBookingIdAsync(bookingId, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Download invoice PDF by ID
    /// </summary>
    [HttpGet("{invoiceId:int}/pdf")]
    public async Task<IActionResult> GetInvoicePdf(
        int invoiceId,
        CancellationToken cancellationToken)
    {
        var result = await _invoiceService.GetInvoicePdfAsync(invoiceId, cancellationToken);
        
        if (result.StatusCode != System.Net.HttpStatusCode.OK || result.Payload is not InvoicePdfResponse pdfResponse)
        {
            return result.ToActionResult();
        }

        return File(pdfResponse.PdfBytes, pdfResponse.ContentType, pdfResponse.FileName);
    }

    /// <summary>
    /// Download invoice PDF by invoice number
    /// </summary>
    [HttpGet("number/{invoiceNumber}/pdf")]
    public async Task<IActionResult> GetInvoicePdfByInvoiceNumber(
        string invoiceNumber,
        CancellationToken cancellationToken)
    {
        var result = await _invoiceService.GetInvoicePdfByInvoiceNumberAsync(invoiceNumber, cancellationToken);
        
        if (result.StatusCode != System.Net.HttpStatusCode.OK || result.Payload is not InvoicePdfResponse pdfResponse)
        {
            return result.ToActionResult();
        }

        return File(pdfResponse.PdfBytes, pdfResponse.ContentType, pdfResponse.FileName);
    }

    /// <summary>
    /// List invoices with pagination and optional payment status filter
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,MasterAdmin,DefaultAdmin")]
    public async Task<IActionResult> ListInvoices(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? paymentStatus = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _invoiceService.ListInvoicesAsync(pageNumber, pageSize, paymentStatus, cancellationToken);
        return result.ToActionResult();
    }
}
