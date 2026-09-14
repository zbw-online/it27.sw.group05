using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.ValidateCustomerDataImport
{
    public interface IValidateCustomerDataImportUseCase
    {
        Task<Result<ValidateCustomerDataImportResponse>> ExecuteAsync(
            ValidateCustomerDataImportQuery query,
            CancellationToken cancellationToken = default);
    }
}
