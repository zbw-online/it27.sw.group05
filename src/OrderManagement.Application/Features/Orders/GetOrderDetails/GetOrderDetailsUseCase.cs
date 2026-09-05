using OrderManagement.Application.Abstractions.Interfaces.Customers.Query;
using OrderManagement.Application.Abstractions.Interfaces.Orders.Query;
using OrderManagement.Application.Features.Orders.Shared;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.GetOrderDetails
{
    public sealed class GetOrderDetailsUseCase(
        IOrderQueryRepository orderQueryRepository,
        ICustomerQueryRepository customerQueryRepository) : IGetOrderDetailsUseCase
    {
        private readonly IOrderQueryRepository _orderQueryRepository = orderQueryRepository;
        private readonly ICustomerQueryRepository _customerQueryRepository = customerQueryRepository;

        public async Task<Result<GetOrderDetailsResponse>> ExecuteAsync(
            GetOrderDetailsQuery query,
            CancellationToken cancellationToken = default)
        {
            Order? order = await _orderQueryRepository.GetByIdAsync(new OrderId(query.OrderId), cancellationToken);
            if (order is null)
            {
                return Results.Fail<GetOrderDetailsResponse>("Auftrag wurde nicht gefunden.");
            }

            Customer? customer = await _customerQueryRepository.GetByIdAsync(order.CustomerId, cancellationToken);

            IReadOnlyList<OrderLineDto> lines = [.. order.Lines
                .OrderBy(l => l.LineNumber)
                .Select(l => new OrderLineDto(
                    l.Id.Value,
                    l.LineNumber,
                    l.ArticleId.Value,
                    l.ArticleName,
                    l.UnitPrice.Amount,
                    l.UnitPrice.Currency,
                    l.Quantity,
                    l.LineTotal.Amount,
                    l.LineTotal.Currency))];

            var response = new GetOrderDetailsResponse(
                order.Id.Value,
                order.OrderNumber.Value,
                order.OrderDate,
                order.DeliveryDate,
                order.CustomerReference,
                order.CustomerId.Value,
                customer?.CustomerNumber.Value ?? string.Empty,
                customer is null ? string.Empty : $"{customer.LastName} {customer.SurName}",
                order.BillingAddress.Street,
                order.BillingAddress.Number,
                order.BillingAddress.PostalCode,
                order.BillingAddress.City,
                order.BillingAddress.Country,
                order.BillingAddressSource,
                order.DeliveryAddress.Street,
                order.DeliveryAddress.Number,
                order.DeliveryAddress.PostalCode,
                order.DeliveryAddress.City,
                order.DeliveryAddress.Country,
                order.DeliveryAddressSource,
                order.Total.Amount,
                order.Total.Currency,
                lines);

            return Results.Success(response);
        }
    }
}
