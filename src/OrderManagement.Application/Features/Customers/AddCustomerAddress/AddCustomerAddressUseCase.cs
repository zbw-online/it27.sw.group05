using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Abstractions.Interfaces.Customers.Command;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.AddCustomerAddress
{
    public sealed class AddCustomerAddressUseCase(
        ICustomerCommandRepository customerCommandRepository,
        IUnitOfWork unitOfWork) : IAddCustomerAddressUseCase
    {
        private readonly ICustomerCommandRepository _customerCommandRepository = customerCommandRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result> ExecuteAsync(
            AddCustomerAddressCommand command,
            CancellationToken cancellationToken = default)
        {
            Customer? customer = await _customerCommandRepository.GetByIdAsync(
                new CustomerId(command.CustomerId),
                cancellationToken);

            if (customer is null)
            {
                return Result.Fail("Customer was not found.");
            }

            Result addressResult = customer.ChangeAddress(
                command.ValidFrom,
                command.Street,
                command.HouseNumber,
                command.PostalCode,
                command.City,
                command.CountryCode);

            if (!addressResult.IsSuccess)
            {
                return addressResult;
            }

            _customerCommandRepository.Update(customer);

            return await _unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
