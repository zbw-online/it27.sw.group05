using System.Globalization;
using System.Text.RegularExpressions;

using OrderManagement.Application.Abstractions.Persistence.Orders.Query;
using OrderManagement.Domain.Orders;

using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.GetNextOrderNumber
{
    public sealed partial class GetNextOrderNumberUseCase(
        IOrderQueryRepository orderQueryRepository) : IGetNextOrderNumberUseCase
    {
        private readonly IOrderQueryRepository _orderQueryRepository = orderQueryRepository;

        public async Task<Result<string>> ExecuteAsync(
            GetNextOrderNumberQuery query,
            CancellationToken cancellationToken = default)
        {
            int year = DateTime.UtcNow.Year;
            IReadOnlyList<Order> orders = await _orderQueryRepository.GetListAsync(cancellationToken);

            int highestSuffix = orders
                .Select(o => SuffixPattern().Match(o.OrderNumber.Value))
                .Where(m => m.Success && int.Parse(m.Groups["year"].Value, CultureInfo.InvariantCulture) == year)
                .Select(m => int.Parse(m.Groups["seq"].Value, CultureInfo.InvariantCulture))
                .DefaultIfEmpty(0)
                .Max();

            string nextNumber = $"ORD-{year}-{highestSuffix + 1:D3}";
            return Results.Success(nextNumber);
        }

        [GeneratedRegex(@"^ORD-(?<year>\d{4})-(?<seq>\d{3})$")]
        private static partial Regex SuffixPattern();
    }
}
