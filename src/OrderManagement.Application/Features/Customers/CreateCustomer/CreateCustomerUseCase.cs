using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Application.Abstractions.Persistence.Customers.Command;
using OrderManagement.Application.Abstractions.Persistence.Customers.Query;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Customers.CreateCustomer
{
    public sealed class CreateCustomerUseCase(
        ICustomerCommandRepository customerCommandRepository,
        ICustomerQueryRepository customerQueryRepository,
        IUnitOfWork unitOfWork) : ICreateCustomerUseCase
    {
        private readonly ICustomerCommandRepository _customerCommandRepository = customerCommandRepository;
        private readonly ICustomerQueryRepository _customerQueryRepository = customerQueryRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result<CreateCustomerResponse>> ExecuteAsync(
            CreateCustomerCommand command,
            CancellationToken cancellationToken = default)
        {
            Result<CustomerNumber> customerNumberResult = CustomerNumber.Create(command.CustomerNumber);
            if (!customerNumberResult.IsSuccess)
            {
                return Results.Fail<CreateCustomerResponse>(customerNumberResult.Error!);
            }

            Customer? existingByNumber = await _customerQueryRepository.GetByCustomerNumberAsync(
                customerNumberResult.Value!,
                cancellationToken);

            if (existingByNumber is not null)
            {
                return Results.Fail<CreateCustomerResponse>(
                    $"Customer number '{command.CustomerNumber}' already exists.");
            }

            Result<Email> emailResult = Email.Create(command.Email);
            if (!emailResult.IsSuccess)
            {
                return Results.Fail<CreateCustomerResponse>(emailResult.Error!);
            }

            Customer? existingByEmail = await _customerQueryRepository.GetByEmailAsync(
                emailResult.Value!,
                cancellationToken);

            if (existingByEmail is not null)
            {
                return Results.Fail<CreateCustomerResponse>(
                    $"Email '{command.Email}' already exists.");
            }

            Result<Customer> customerResult = Customer.Create(
                command.CustomerNumber,
                command.LastName,
                command.SurName,
                command.Email,
                command.Website);

            if (!customerResult.IsSuccess)
            {
                return Results.Fail<CreateCustomerResponse>(customerResult.Error!);
            }

            Customer customer = customerResult.Value!;

            Result addressResult = customer.ChangeAddress(
                command.AddressValidFrom,
                command.Street,
                command.HouseNumber,
                command.PostalCode,
                command.City,
                command.CountryCode);

            if (!addressResult.IsSuccess)
            {
                return Results.Fail<CreateCustomerResponse>(addressResult.Error!);
            }

            _customerCommandRepository.Add(customer);

            Result commitResult = await _unitOfWork.CommitAsync(cancellationToken);
            return !commitResult.IsSuccess
                ? Results.Fail<CreateCustomerResponse>(commitResult.Error!)
                : Results.Success(new CreateCustomerResponse(
                customer.Id.Value,
                customer.CustomerNumber.Value,
                $"{customer.LastName} {customer.SurName}",
                customer.Email.Value));
        }
    }
}
