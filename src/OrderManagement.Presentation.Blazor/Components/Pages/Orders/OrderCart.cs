using OrderManagement.Application.Features.Catalog.Contracts;
using OrderManagement.Application.Features.Orders.Contracts;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Presentation.Blazor.Components.Pages.Orders
{
    public sealed class OrderCartLine
    {
        public required int ArticleId { get; init; }
        public required string ArticleNumber { get; init; }
        public required string ArticleName { get; init; }
        public required decimal UnitPriceAmount { get; init; }
        public required string Currency { get; init; }
        public required decimal VatRate { get; init; }
        public required int Stock { get; init; }
        public required StockLevel StockLevel { get; init; }
        public int Quantity { get; set; } = 1;
    }

    public sealed class OrderCart
    {
        private readonly Dictionary<int, OrderCartLine> _lines = [];

        public IReadOnlyList<OrderCartLine> Lines => [.. _lines.Values];

        public int Count => _lines.Count;

        public OrderDraftTotalsDto Totals { get; private set; } = new(0, 0, 0, "CHF");

        public void Add(ArticleListItemDto article)
        {
            if (_lines.TryGetValue(article.ArticleId, out OrderCartLine? existing))
            {
                existing.Quantity += 1;
            }
            else
            {
                _lines[article.ArticleId] = new OrderCartLine
                {
                    ArticleId = article.ArticleId,
                    ArticleNumber = article.ArticleNumber,
                    ArticleName = article.Name,
                    UnitPriceAmount = article.PriceAmount,
                    Currency = article.PriceCurrency,
                    VatRate = article.VatRate,
                    Stock = article.Stock,
                    StockLevel = article.StockLevel,
                    Quantity = 1
                };
            }

            Recalculate();
        }

        public void Remove(int articleId)
        {
            _ = _lines.Remove(articleId);
            Recalculate();
        }

        public void ChangeQuantity(int articleId, int delta)
        {
            if (!_lines.TryGetValue(articleId, out OrderCartLine? line))
            {
                return;
            }

            int newQuantity = line.Quantity + delta;
            if (newQuantity < 1)
            {
                return;
            }

            line.Quantity = newQuantity;
            Recalculate();
        }

        public void SetQuantity(int articleId, int quantity)
        {
            if (quantity < 1 || !_lines.TryGetValue(articleId, out OrderCartLine? line))
            {
                return;
            }

            line.Quantity = quantity;
            Recalculate();
        }

        private void Recalculate()
        {
            IReadOnlyList<OrderDraftLineInput> inputs = [.. _lines.Values
                .Select(l => new OrderDraftLineInput(l.UnitPriceAmount, l.Currency, l.Quantity, l.VatRate))];

            Result<OrderDraftTotalsDto> result = OrderDraftCalculator.Calculate(inputs);
            Totals = result.IsSuccess ? result.Value! : new OrderDraftTotalsDto(0, 0, 0, "CHF");
        }
    }
}
