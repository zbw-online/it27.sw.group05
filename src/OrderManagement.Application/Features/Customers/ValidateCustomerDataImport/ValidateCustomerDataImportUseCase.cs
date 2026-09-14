using OrderManagement.Application.Features.Customers.DataExchange.Contracts;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.ValidateCustomerDataImport
{
    public sealed class ValidateCustomerDataImportUseCase(ICustomerImportPlanBuilder planBuilder) : IValidateCustomerDataImportUseCase
    {
        private readonly ICustomerImportPlanBuilder _planBuilder = planBuilder;

        public async Task<Result<ValidateCustomerDataImportResponse>> ExecuteAsync(
            ValidateCustomerDataImportQuery query,
            CancellationToken cancellationToken = default)
        {
            CustomerImportPlan plan = await _planBuilder.BuildAsync(query.File, cancellationToken);

            return Results.Success(new ValidateCustomerDataImportResponse(plan.IsValid, plan.TotalRecordCount, plan.Issues));
        }
    }
}
