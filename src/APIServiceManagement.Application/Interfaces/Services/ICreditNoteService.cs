using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.DTOs.Responses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Application.Interfaces.Services;

public interface ICreditNoteService
{
    Task<ServiceResult> CreateCreditNoteAsync(int invoiceId, CreateCreditNoteRequest request, Guid createdBy, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetCreditNoteByIdAsync(int creditNoteId, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetCreditNoteByNumberAsync(string creditNoteNumber, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetCreditNotesByInvoiceIdAsync(int invoiceId, CancellationToken cancellationToken = default);
    Task<ServiceResult> ListCreditNotesAsync(int? pageNumber, int? pageSize, string? status, DateTime? startDate, DateTime? endDate, Guid? customerId, CancellationToken cancellationToken = default);
    Task<ServiceResult> ApplyCreditNoteAsync(int creditNoteId, ApplyCreditNoteRequest request, Guid appliedBy, CancellationToken cancellationToken = default);
    Task<ServiceResult> CancelCreditNoteAsync(int creditNoteId, string? reason, Guid cancelledBy, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetCreditNotePdfAsync(int creditNoteId, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetCreditNoteSummaryReportAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetCreditNotesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<ServiceResult> GetCreditNotesByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
}
