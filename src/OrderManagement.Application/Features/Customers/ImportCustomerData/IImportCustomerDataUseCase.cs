using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.ImportCustomerData
{
    public interface IImportCustomerDataUseCase
    {
        Task<Result<ImportCustomerDataResponse>> ExecuteAsync(
            ImportCustomerDataCommand command,
            CancellationToken cancellationToken = default);
    }
}
