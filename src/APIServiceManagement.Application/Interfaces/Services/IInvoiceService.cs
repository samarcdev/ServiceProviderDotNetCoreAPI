using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Application.Interfaces.Services;

public interface IInvoiceService
{
    Task<ServiceResult> GenerateInvoiceAsync(Guid bookingId, GenerateInvoiceRequest? request, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetInvoiceByIdAsync(int invoiceId, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetInvoiceByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetInvoiceByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetInvoicePdfAsync(int invoiceId, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetInvoicePdfByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);
    Task<ServiceResult> ListInvoicesAsync(int pageNumber = 1, int pageSize = 10, string? paymentStatus = null, CancellationToken cancellationToken = default);
}
