using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Abstractions.Interfaces.Customers.Command;
using OrderManagement.Application.Abstractions.Interfaces.Customers.Query;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.UpdateCustomer
{
    public sealed class UpdateCustomerUseCase(
        ICustomerCommandRepository customerCommandRepository,
        ICustomerQueryRepository customerQueryRepository,
        IUnitOfWork unitOfWork) : IUpdateCustomerUseCase
    {
        private readonly ICustomerCommandRepository _customerCommandRepository = customerCommandRepository;
        private readonly ICustomerQueryRepository _customerQueryRepository = customerQueryRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result> ExecuteAsync(
            UpdateCustomerCommand command,
            CancellationToken cancellationToken = default)
        {
            Customer? customer = await _customerCommandRepository.GetByIdAsync(
                new CustomerId(command.CustomerId),
                cancellationToken);

            if (customer is null)
            {
                return Result.Fail("Customer was not found.");
            }

            Result<Email> emailResult = Email.Create(command.Email);
            if (!emailResult.IsSuccess)
            {
                return Result.Fail(emailResult.Error!);
            }

            Customer? existingByEmail = await _customerQueryRepository.GetByEmailAsync(
                emailResult.Value!,
                cancellationToken);

            if (existingByEmail is not null && existingByEmail.Id != customer.Id)
            {
                return Result.Fail($"Email '{command.Email}' already exists.");
            }

            Result nameResult = customer.ChangeName(command.LastName, command.SurName);
            if (!nameResult.IsSuccess)
            {
                return nameResult;
            }

            Result emailChangeResult = customer.ChangeEmail(command.Email);
            if (!emailChangeResult.IsSuccess)
            {
                return emailChangeResult;
            }

            Result websiteResult = customer.ChangeWebsite(command.Website);
            if (!websiteResult.IsSuccess)
            {
                return websiteResult;
            }

            Result addressResult = customer.ChangeAddress(
                command.AddressValidFrom,
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
