using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Abstractions.Interfaces.Catalog.Query;
using OrderManagement.Application.Abstractions.Interfaces.Customers.Query;
using OrderManagement.Application.Abstractions.Interfaces.Orders.Command;
using OrderManagement.Application.Abstractions.Interfaces.Orders.Query;
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
        IArticleQueryRepository articleQueryRepository,
        IUnitOfWork unitOfWork) : ICreateOrderUseCase
    {
        private readonly IOrderCommandRepository _orderCommandRepository = orderCommandRepository;
        private readonly IOrderQueryRepository _orderQueryRepository = orderQueryRepository;
        private readonly ICustomerQueryRepository _customerQueryRepository = customerQueryRepository;
        private readonly IArticleQueryRepository _articleQueryRepository = articleQueryRepository;
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

            Result<Address> addressResult = Address.Create(
                command.Street, command.HouseNumber, command.PostalCode, command.City, command.CountryCode);

            if (!addressResult.IsSuccess)
            {
                return Results.Fail<CreateOrderResponse>(addressResult.Error!);
            }

            Result<Order> orderResult = Order.Create(command.OrderNumber, customerId, addressResult.Value!);
            if (!orderResult.IsSuccess)
            {
                return Results.Fail<CreateOrderResponse>(orderResult.Error!);
            }

            Order order = orderResult.Value!;

            foreach (CreateOrderLineInput lineInput in command.Lines)
            {
                Article? article = await _articleQueryRepository.GetByIdAsync(
                    new ArticleId(lineInput.ArticleId),
                    cancellationToken);

                if (article is null)
                {
                    return Results.Fail<CreateOrderResponse>(
                        $"Article with id '{lineInput.ArticleId}' was not found.");
                }

                Result addLineResult = order.AddLine(article.Id, article.Name, article.Price, lineInput.Quantity);
                if (!addLineResult.IsSuccess)
                {
                    return Results.Fail<CreateOrderResponse>(addLineResult.Error!);
                }
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
    }
}
