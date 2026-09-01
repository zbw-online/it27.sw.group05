using OrderManagement.Domain.Catalog.Events;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.Primitives;
using SharedKernel.SeedWork;

namespace OrderManagement.Domain.Catalog
{
    public sealed class Article : AggregateRoot<ArticleId>
    {
        private Article() : base(ArticleId.Empty)
        {
            // EF Core
        }

        private Article(
            ArticleNumber number,
            string name,
            Money price,
            ArticleGroupId groupId
            ) : base(ArticleId.Empty)
        {
            ArticleNumber = number;
            Name = name;
            Price = price;
            ArticleGroupId = groupId;

            AddDomainEvent(new ArticleCreated(number, DateTime.UtcNow));
        }

        public ArticleNumber ArticleNumber { get; private set; } = default!;
        public string Name { get; private set; } = default!;
        public Money Price { get; private set; } = default!;
        public ArticleGroupId ArticleGroupId { get; private set; }
        public int Stock { get; private set; }
        public int ReorderPoint { get; private set; }
        public decimal VatRate { get; private set; }
        public string? Description { get; private set; }
        public ArticleStatus Status { get; private set; } = ArticleStatus.Active;

        public StockLevel StockLevel => Stock == 0
            ? StockLevel.OutOfStock
            : Stock <= ReorderPoint
                ? StockLevel.Low
                : StockLevel.Available;

        public static Result<Article> Create(
            string? articleNr,
            string? name,
            decimal priceAmount,
            string priceCurrency,
            ArticleGroupId groupId,
            int stock = 0,
            int reorderPoint = 20,
            decimal vatRate = 0.0m,
            string? description = null,
            ArticleStatus status = ArticleStatus.Active
            )
        {
            if (!string.IsNullOrEmpty(articleNr) && articleNr.Length > 20) return Results.Fail<Article>("ArticleNumber cannot be empty or exceed 20 characters.");
            Result<ArticleNumber> nr = ArticleNumber.Create(articleNr);
            if (!nr.IsSuccess) return Results.Fail<Article>(nr.Error!);

            string trimmedName = (name ?? string.Empty).Trim();
            if (trimmedName.Length == 0) return Results.Fail<Article>("Name is required.");
            if (trimmedName.Length > 200) return Results.Fail<Article>("Name cannot exceed 200 characters.");

            if (!groupId.IsAssigned)
                return Results.Fail<Article>("ArticleGroupId must be assigned.");

            if (stock < 0) return Results.Fail<Article>("Stock cannot be negative.");

            if (reorderPoint < 0) return Results.Fail<Article>("ReorderPoint cannot be negative.");

            if (vatRate is < 0 or > 999.99m) return Results.Fail<Article>("VatRate must be between 0 and 999.99.");

            if (Math.Floor(vatRate * 100) / 100 != vatRate) return Results.Fail<Article>("VatRate must have at most 2 decimal places.");

            Money priceValue = Money.From(priceAmount, priceCurrency).EnsureValue();

            var article = new Article(
                nr.Value!,
                trimmedName,
                priceValue,
                groupId
            )
            {
                Stock = stock,
                ReorderPoint = reorderPoint,
                VatRate = vatRate,
                Description = description,
                Status = status
            };

            return Results.Success(article);
        }

        public Result ChangePrice(Money newPrice)
        {
            if (newPrice.Amount < 0)
                return Result.Fail("Price cannot be negative.");

            Money oldPrice = Price;
            Price = newPrice;

            AddDomainEvent(new ArticlePriceChanged(ArticleNumber, oldPrice, newPrice, DateTime.UtcNow));
            return Result.Success();
        }


        public Result UpdateStock(int delta)
        {
            if (delta < 0 && Stock + delta < 0)
                return Result.Fail("Cannot reduce stock below zero.");

            int oldStock = Stock;
            Stock += delta;

            AddDomainEvent(new ArticleStockChanged(ArticleNumber, oldStock, Stock, DateTime.UtcNow));
            return Result.Success();
        }


        public Result ChangeReorderPoint(int newReorderPoint)
        {
            if (newReorderPoint < 0)
                return Result.Fail("ReorderPoint cannot be negative.");

            int oldReorderPoint = ReorderPoint;
            ReorderPoint = newReorderPoint;

            AddDomainEvent(new ArticleReorderPointChanged(ArticleNumber, oldReorderPoint, newReorderPoint, DateTime.UtcNow));
            return Result.Success();
        }

        public Result ChangeGroup(ArticleGroupId newGroupId)
        {
            if (!newGroupId.IsAssigned)
                return Result.Fail("ArticleGroupId must be assigned.");

            ArticleGroupId oldGroupId = ArticleGroupId;
            ArticleGroupId = newGroupId;

            AddDomainEvent(new ArticleMovedToGroup(ArticleNumber, oldGroupId, newGroupId, DateTime.UtcNow));
            return Result.Success();
        }

        public Result Deactivate()
        {
            if (Status == ArticleStatus.Inactive)
                return Result.Fail("Article is already inactive.");

            Status = ArticleStatus.Inactive;
            AddDomainEvent(new ArticleDeactivated(ArticleNumber, DateTime.UtcNow));
            return Result.Success();
        }

        public Result Reactivate()
        {
            if (Status == ArticleStatus.Active)
                return Result.Fail("Article is already active.");

            Status = ArticleStatus.Active;
            AddDomainEvent(new ArticleReactivated(ArticleNumber, DateTime.UtcNow));
            return Result.Success();
        }

        public Result EnsureAvailableForOrder()
            => Status == ArticleStatus.Active
                ? Result.Success()
                : Result.Fail($"Artikel '{Name}' ist deaktiviert und kann nicht bestellt werden.");
    }
}
