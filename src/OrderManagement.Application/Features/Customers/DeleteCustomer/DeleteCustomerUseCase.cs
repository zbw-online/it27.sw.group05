using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Abstractions.Interfaces.Customers.Command;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.DeleteCustomer
{
    public sealed class DeleteCustomerUseCase(
        ICustomerCommandRepository customerCommandRepository,
        IUnitOfWork unitOfWork) : IDeleteCustomerUseCase
    {
        private readonly ICustomerCommandRepository _customerCommandRepository = customerCommandRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result> ExecuteAsync(
            DeleteCustomerCommand command,
            CancellationToken cancellationToken = default)
        {
            Customer? customer = await _customerCommandRepository.GetByIdAsync(
                new CustomerId(command.CustomerId),
                cancellationToken);

            if (customer is null)
            {
                return Result.Fail("Customer was not found.");
            }

            _customerCommandRepository.Remove(customer);

            return await _unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
