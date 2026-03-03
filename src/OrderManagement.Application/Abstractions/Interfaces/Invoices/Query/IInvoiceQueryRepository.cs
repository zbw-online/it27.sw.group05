using OrderManagement.Application.DTOs.Invoices;

namespace OrderManagement.Application.Abstractions.Interfaces.Invoices.Query
{
    public interface IInvoiceQueryRepository
    {
        Task<IReadOnlyList<InvoiceDto>> GetOrdersWithHistoricalAddressAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? customerNumber = null,
            CancellationToken ct = default);
    }
}
