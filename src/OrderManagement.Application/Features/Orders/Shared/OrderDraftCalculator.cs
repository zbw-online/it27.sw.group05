using SharedKernel.Primitives;

namespace OrderManagement.Application.Features.Orders.Shared
{
    public static class OrderDraftCalculator
    {
        public static Result<OrderDraftTotalsDto> Calculate(IReadOnlyList<OrderDraftLineInput> lines)
        {
            if (lines.Count == 0)
            {
                return Results.Success(new OrderDraftTotalsDto(0m, 0m, 0m, "CHF"));
            }

            string currency = lines[0].Currency;

            if (lines.Any(l => !string.Equals(l.Currency, currency, StringComparison.OrdinalIgnoreCase)))
            {
                return Results.Fail<OrderDraftTotalsDto>("Alle Positionen müssen dieselbe Währung verwenden.");
            }

            if (lines.Any(l => l.Quantity <= 0))
            {
                return Results.Fail<OrderDraftTotalsDto>("Menge muss positiv sein.");
            }

            Money subtotal = Money.From(0, currency).EnsureValue();
            Money vatTotal = Money.From(0, currency).EnsureValue();

            foreach (OrderDraftLineInput line in lines)
            {
                Result<Money> lineTotalResult = Money.From(line.UnitPriceAmount * line.Quantity, currency);
                if (!lineTotalResult.IsSuccess)
                {
                    return Results.Fail<OrderDraftTotalsDto>(lineTotalResult.Error!);
                }

                Money lineTotal = lineTotalResult.Value!;
                subtotal += lineTotal;

                Result<Money> lineVatResult = Money.From(lineTotal.Amount * line.VatRate / 100m, currency);
                if (!lineVatResult.IsSuccess)
                {
                    return Results.Fail<OrderDraftTotalsDto>(lineVatResult.Error!);
                }

                vatTotal += lineVatResult.Value!;
            }

            Money total = subtotal + vatTotal;

            return Results.Success(new OrderDraftTotalsDto(subtotal.Amount, vatTotal.Amount, total.Amount, currency));
        }
    }
}
