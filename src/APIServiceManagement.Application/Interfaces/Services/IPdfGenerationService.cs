using APIServiceManagement.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Application.Interfaces.Services;

public interface IPdfGenerationService
{
    Task<byte[]> GenerateInvoicePdfAsync(InvoiceResponse invoice, CancellationToken cancellationToken = default);
}
