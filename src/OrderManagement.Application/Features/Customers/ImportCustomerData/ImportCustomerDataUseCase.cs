using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Abstractions.Interfaces.Customers.Command;
using OrderManagement.Application.Features.Customers.DataExchange.Shared;
using OrderManagement.Domain.Customers;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.ImportCustomerData
{
    public sealed class ImportCustomerDataUseCase(
        ICustomerImportPlanBuilder planBuilder,
        ICustomerCommandRepository customerCommandRepository,
        IUnitOfWork unitOfWork) : IImportCustomerDataUseCase
    {
        private readonly ICustomerImportPlanBuilder _planBuilder = planBuilder;
        private readonly ICustomerCommandRepository _customerCommandRepository = customerCommandRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result<ImportCustomerDataResponse>> ExecuteAsync(
            ImportCustomerDataCommand command,
            CancellationToken cancellationToken = default)
        {
            CustomerImportPlan plan = await _planBuilder.BuildAsync(command.File, cancellationToken);

            if (!plan.IsValid)
            {
                return Results.Success(new ImportCustomerDataResponse(false, 0, plan.TotalRecordCount, plan.Issues));
            }

            foreach (Customer customer in plan.CustomersToImport)
            {
                _customerCommandRepository.Add(customer);
            }

            Result commitResult = await _unitOfWork.CommitAsync(cancellationToken);
            return !commitResult.IsSuccess
                ? Results.Fail<ImportCustomerDataResponse>(commitResult.Error!)
                : Results.Success(new ImportCustomerDataResponse(true, plan.CustomersToImport.Count, plan.TotalRecordCount, plan.Issues));
        }
    }
}
