using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Application.Abstractions.Persistence.Catalog.Command;
using OrderManagement.Application.Abstractions.Persistence.Customers.Query;
using OrderManagement.Application.Abstractions.Persistence.Orders.Command;
using OrderManagement.Application.Abstractions.Persistence.Orders.Query;
using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.CreateOrder
{
    public sealed class CreateOrderUseCase(
        IOrderCommandRepository orderCommandRepository,
        IOrderQueryRepository orderQueryRepository,
        ICustomerQueryRepository customerQueryRepository,
        IArticleCommandRepository articleCommandRepository,
        IUnitOfWork unitOfWork) : ICreateOrderUseCase
    {
        private readonly IOrderCommandRepository _orderCommandRepository = orderCommandRepository;
        private readonly IOrderQueryRepository _orderQueryRepository = orderQueryRepository;
        private readonly ICustomerQueryRepository _customerQueryRepository = customerQueryRepository;
        private readonly IArticleCommandRepository _articleCommandRepository = articleCommandRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result<CreateOrderResponse>> ExecuteAsync(
            CreateOrderCommand command,
            CancellationToken cancellationToken = default)
        {
            Result<OrderNumber> numberResult = OrderNumber.Create(command.OrderNumber);
            if (!numberResult.IsSuccess)
            {
                return Results.Fail<CreateOrderResponse>(numberResult.Error!);
            }

            Order? existingByNumber = await _orderQueryRepository.GetByOrderNumberAsync(
                numberResult.Value!,
                cancellationToken);

            if (existingByNumber is not null)
            {
                return Results.Fail<CreateOrderResponse>(
                    $"Order number '{command.OrderNumber}' already exists.");
            }

            var customerId = new CustomerId(command.CustomerId);
            Customer? customer = await _customerQueryRepository.GetByIdAsync(customerId, cancellationToken);
            if (customer is null)
            {
                return Results.Fail<CreateOrderResponse>("Customer was not found.");
            }

            if (command.Lines.Count == 0)
            {
                return Results.Fail<CreateOrderResponse>("Ein Auftrag benötigt mindestens eine gültige Position.");
            }

            Result<(Address Address, AddressSource Source)> billingResult = ResolveAddress(
                customer, command.DeliveryDate, command.BillingAddressOverride);
            if (!billingResult.IsSuccess)
            {
                return Results.Fail<CreateOrderResponse>(billingResult.Error!);
            }

            Result<(Address Address, AddressSource Source)> deliveryResult = ResolveAddress(
                customer, command.DeliveryDate, command.DeliveryAddressOverride);
            if (!deliveryResult.IsSuccess)
            {
                return Results.Fail<CreateOrderResponse>(deliveryResult.Error!);
            }

            (Address billingAddress, AddressSource billingSource) = billingResult.Value!;
            (Address deliveryAddress, AddressSource deliverySource) = deliveryResult.Value!;

            Result<Order> orderResult = Order.Create(
                command.OrderNumber,
                customerId,
                command.DeliveryDate,
                billingAddress,
                billingSource,
                deliveryAddress,
                deliverySource,
                command.CustomerReference);
            if (!orderResult.IsSuccess)
            {
                return Results.Fail<CreateOrderResponse>(orderResult.Error!);
            }

            Order order = orderResult.Value!;

            foreach (CreateOrderLineInput lineInput in command.Lines)
            {
                Article? article = await _articleCommandRepository.GetByIdAsync(
                    new ArticleId(lineInput.ArticleId),
                    cancellationToken);

                if (article is null)
                {
                    return Results.Fail<CreateOrderResponse>(
                        $"Article with id '{lineInput.ArticleId}' was not found.");
                }

                Result availabilityResult = article.EnsureAvailableForOrder();
                if (!availabilityResult.IsSuccess)
                {
                    return Results.Fail<CreateOrderResponse>(availabilityResult.Error!);
                }

                Result addLineResult = order.AddLine(article.Id, article.Name, article.Price, lineInput.Quantity);
                if (!addLineResult.IsSuccess)
                {
                    return Results.Fail<CreateOrderResponse>(addLineResult.Error!);
                }

                Result stockResult = article.UpdateStock(-lineInput.Quantity);
                if (!stockResult.IsSuccess)
                {
                    return Results.Fail<CreateOrderResponse>(stockResult.Error!);
                }

                _articleCommandRepository.Update(article);
            }

            Result markInventoryAppliedResult = order.MarkInventoryApplied();
            if (!markInventoryAppliedResult.IsSuccess)
            {
                return Results.Fail<CreateOrderResponse>(markInventoryAppliedResult.Error!);
            }

            _orderCommandRepository.Add(order);

            Result commitResult = await _unitOfWork.CommitAsync(cancellationToken);
            return !commitResult.IsSuccess
                ? Results.Fail<CreateOrderResponse>(commitResult.Error!)
                : Results.Success(new CreateOrderResponse(
                    order.Id.Value,
                    order.OrderNumber.Value,
                    order.Total.Amount,
                    order.Total.Currency));
        }

        private static Result<(Address Address, AddressSource Source)> ResolveAddress(
            Customer customer,
            DateOnly deliveryDate,
            AddressOverrideInput? overrideInput)
        {
            if (overrideInput is not null)
            {
                Result<Address> manualResult = Address.Create(
                    overrideInput.Street,
                    overrideInput.HouseNumber,
                    overrideInput.PostalCode,
                    overrideInput.City,
                    overrideInput.CountryCode);

                return !manualResult.IsSuccess
                    ? Results.Fail<(Address, AddressSource)>(manualResult.Error!)
                    : Results.Success((manualResult.Value!, AddressSource.Manual));
            }

            CustomerAddress? customerAddress = customer.AddressAt(deliveryDate);
            if (customerAddress is null)
            {
                return Results.Fail<(Address, AddressSource)>(
                    "Für den gewählten Liefertermin ist keine gültige Kundenadresse hinterlegt. Bitte erfassen Sie eine manuelle Adresse.");
            }

            Result<Address> automaticResult = Address.Create(
                customerAddress.Street,
                customerAddress.HouseNumber,
                customerAddress.PostalCode,
                customerAddress.City,
                customerAddress.CountryCode);

            return !automaticResult.IsSuccess
                ? Results.Fail<(Address, AddressSource)>(automaticResult.Error!)
                : Results.Success((automaticResult.Value!, AddressSource.Automatic));
        }
    }
}
