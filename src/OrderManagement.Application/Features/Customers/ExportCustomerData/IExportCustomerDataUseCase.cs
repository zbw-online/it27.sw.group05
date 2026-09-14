using OrderManagement.Application.Features.Customers.DataExchange.Contracts;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.ExportCustomerData
{
    public interface IExportCustomerDataUseCase
    {
        Task<Result<CustomerDataFile>> ExecuteAsync(
            ExportCustomerDataQuery query,
            CancellationToken cancellationToken = default);
    }
}
