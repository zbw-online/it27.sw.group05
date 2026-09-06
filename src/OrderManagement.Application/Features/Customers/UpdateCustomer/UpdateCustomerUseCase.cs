using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Application.Abstractions.Persistence.Customers.Command;
using OrderManagement.Application.Abstractions.Persistence.Customers.Query;
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

            CustomerAddress? currentAddress = customer.AddressAt(DateOnly.FromDateTime(DateTime.Today));
            bool addressChanged = currentAddress is null ||
                currentAddress.ValidFrom != command.AddressValidFrom ||
                !string.Equals(currentAddress.Street, command.Street.Trim(), StringComparison.Ordinal) ||
                !string.Equals(currentAddress.HouseNumber, command.HouseNumber.Trim(), StringComparison.Ordinal) ||
                !string.Equals(currentAddress.PostalCode, command.PostalCode.Trim(), StringComparison.Ordinal) ||
                !string.Equals(currentAddress.City, command.City.Trim(), StringComparison.Ordinal) ||
                !string.Equals(currentAddress.CountryCode, command.CountryCode.Trim().ToUpperInvariant(), StringComparison.Ordinal);

            if (addressChanged)
            {
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
            }

            _customerCommandRepository.Update(customer);

            return await _unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
