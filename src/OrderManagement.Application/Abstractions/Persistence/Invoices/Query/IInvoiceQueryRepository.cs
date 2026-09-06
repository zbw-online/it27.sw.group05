using OrderManagement.Application.Features.Invoices.Contracts;

namespace OrderManagement.Application.Abstractions.Persistence.Invoices.Query
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
