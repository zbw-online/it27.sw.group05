using Microsoft.EntityFrameworkCore;

using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.Persistence.Initialization
{
    /// <summary>
    /// Seeds a deterministic, idempotent set of demonstration data using only Domain
    /// creation/mutation methods. Safe to invoke repeatedly against the same database:
    /// existing demo records (matched by their stable business identifiers) are left
    /// untouched, and a conflicting existing record throws instead of overwriting data.
    /// </summary>
    public sealed class DemoDataSeeder(OrderManagementDbContext dbContext, TimeProvider timeProvider)
    {
        private readonly OrderManagementDbContext _dbContext = dbContext;
        private readonly TimeProvider _timeProvider = timeProvider;

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

            IReadOnlyDictionary<string, Customer> customers = await SeedCustomersAsync(today, cancellationToken);
            IReadOnlyDictionary<string, ArticleGroup> leafCategories = await SeedCategoriesAsync(cancellationToken);
            IReadOnlyDictionary<string, Article> articles = await SeedArticlesAsync(leafCategories, cancellationToken);
            await SeedOrdersAsync(customers, articles, cancellationToken);
        }

        private async Task<IReadOnlyDictionary<string, Customer>> SeedCustomersAsync(DateOnly today, CancellationToken ct)
        {
            (string Number, string LastName, string SurName, string Email, string Street, string HouseNumber, string PostalCode, string City)[] seedCustomers =
            [
                ("CU00001", "Müller", "Anna", "anna.mueller@example.ch", "Bahnhofstrasse", "12", "8001", "Zürich"),
                ("CU00002", "Meier", "Peter", "peter.meier@example.ch", "Marktgasse", "5", "3011", "Bern"),
                ("CU00003", "Keller", "Sara", "sara.keller@example.ch", "Rheinsprung", "3", "4051", "Basel"),
                ("CU00004", "Fischer", "Reto", "reto.fischer@example.ch", "Rue du Rhône", "20", "1204", "Genf"),
                ("CU00005", "Weber", "Nina", "nina.weber@example.ch", "Via Nassa", "8", "6900", "Lugano"),
            ];

            var result = new Dictionary<string, Customer>();

            foreach ((string Number, string LastName, string SurName, string Email, string Street, string HouseNumber, string PostalCode, string City) seed in seedCustomers)
            {
                Domain.Customers.ValueObjects.CustomerNumber customerNumber =
                    Domain.Customers.ValueObjects.CustomerNumber.Create(seed.Number).EnsureValue();

                Customer? existing = await _dbContext.Customers
                    .Include(c => c.Addresses)
                    .FirstOrDefaultAsync(c => c.CustomerNumber == customerNumber, ct);

                if (existing is not null)
                {
                    if (!string.Equals(existing.LastName, seed.LastName, StringComparison.Ordinal) ||
                        !string.Equals(existing.SurName, seed.SurName, StringComparison.Ordinal) ||
                        !string.Equals(existing.Email.Value, seed.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"Demo-Kunde '{seed.Number}' existiert bereits mit abweichenden Daten.");
                    }

                    result[seed.Number] = existing;
                    continue;
                }

                Customer customer = Customer.Create(seed.Number, seed.LastName, seed.SurName, seed.Email, null).EnsureValue();

                // Historical -> current -> future address, so every demo customer demonstrates
                // the full validity-period lifecycle (Customer.ChangeAddress closes the previous one).
                customer.ChangeAddress(today.AddYears(-2), seed.Street, seed.HouseNumber, seed.PostalCode, seed.City, "CH").EnsureSuccess();
                customer.ChangeAddress(today.AddMonths(-6), seed.Street, seed.HouseNumber, seed.PostalCode, seed.City, "CH").EnsureSuccess();
                customer.ChangeAddress(today.AddMonths(3), seed.Street, seed.HouseNumber, seed.PostalCode, seed.City, "CH").EnsureSuccess();

                _ = _dbContext.Customers.Add(customer);
                result[seed.Number] = customer;
            }

            _ = await _dbContext.SaveChangesAsync(ct);
            return result;
        }

        private async Task<IReadOnlyDictionary<string, ArticleGroup>> SeedCategoriesAsync(CancellationToken ct)
        {
            var leaves = new Dictionary<string, ArticleGroup>();

            ArticleGroup elektronik = await EnsureCategoryAsync("Elektronik", null, ct);
            ArticleGroup computerZubehoer = await EnsureCategoryAsync("Computer & Zubehör", elektronik, ct);
            leaves["Laptops"] = await EnsureCategoryAsync("Laptops", computerZubehoer, ct);
            leaves["Tastaturen & Mäuse"] = await EnsureCategoryAsync("Tastaturen & Mäuse", computerZubehoer, ct);
            leaves["Mobiltelefone"] = await EnsureCategoryAsync("Mobiltelefone", elektronik, ct);

            ArticleGroup bueromaterial = await EnsureCategoryAsync("Büromaterial", null, ct);
            leaves["Schreibwaren"] = await EnsureCategoryAsync("Schreibwaren", bueromaterial, ct);
            leaves["Papier"] = await EnsureCategoryAsync("Papier", bueromaterial, ct);

            return leaves;
        }

        private async Task<ArticleGroup> EnsureCategoryAsync(string name, ArticleGroup? parent, CancellationToken ct)
        {
            ArticleGroupId? parentId = parent?.Id;

            ArticleGroup? existing = await _dbContext.ArticleGroups
                .FirstOrDefaultAsync(g => g.Name == name && g.ParentGroupId == parentId, ct);

            if (existing is not null)
            {
                return existing;
            }

            ArticleGroup group = ArticleGroup.Create(name, parentId).EnsureValue();
            _ = _dbContext.ArticleGroups.Add(group);
            _ = await _dbContext.SaveChangesAsync(ct);

            return group;
        }

        private async Task<IReadOnlyDictionary<string, Article>> SeedArticlesAsync(
            IReadOnlyDictionary<string, ArticleGroup> leafCategories,
            CancellationToken ct)
        {
            (string Number, string Name, decimal Price, string Category, int Stock, int ReorderPoint)[] seedArticles =
            [
                ("ART-00001", "Business Laptop 14 Zoll", 1199.00m, "Laptops", 15, 5),
                ("ART-00002", "Ultrabook 13 Kompakt", 1549.00m, "Laptops", 4, 5),
                ("ART-00003", "Kabellose Tastatur Comfort", 39.90m, "Tastaturen & Mäuse", 50, 10),
                ("ART-00004", "Ergonomische Maus Pro", 29.90m, "Tastaturen & Mäuse", 8, 10),
                ("ART-00005", "Smartphone Modell X", 799.00m, "Mobiltelefone", 20, 8),
                ("ART-00006", "Smartphone Modell Lite", 449.00m, "Mobiltelefone", 6, 6),
                ("ART-00007", "Kugelschreiber-Set (10 Stk.)", 6.50m, "Schreibwaren", 200, 30),
                ("ART-00008", "Textmarker-Set Bunt", 4.90m, "Schreibwaren", 15, 20),
                ("ART-00009", "Kopierpapier A4 (500 Blatt)", 8.90m, "Papier", 100, 25),
                ("ART-00010", "Notizblock A5 Liniert", 3.90m, "Papier", 12, 15),
            ];

            var result = new Dictionary<string, Article>();

            foreach ((string articleNumberValue, string name, decimal price, string categoryName, int stock, int reorderPoint) in seedArticles)
            {
                ArticleNumber number = ArticleNumber.Create(articleNumberValue).EnsureValue();
                ArticleGroup category = leafCategories[categoryName];

                Article? existing = await _dbContext.Articles
                    .FirstOrDefaultAsync(a => a.ArticleNumber.Value == number.Value, ct);

                if (existing is not null)
                {
                    if (!string.Equals(existing.Name, name, StringComparison.Ordinal) ||
                        existing.Price.Amount != price ||
                        existing.ArticleGroupId != category.Id)
                    {
                        throw new InvalidOperationException(
                            $"Demo-Artikel '{articleNumberValue}' existiert bereits mit abweichenden Daten.");
                    }

                    result[articleNumberValue] = existing;
                    continue;
                }

                Article article = Article.Create(
                    articleNumberValue,
                    name,
                    price,
                    "CHF",
                    category.Id,
                    stock: stock,
                    reorderPoint: reorderPoint).EnsureValue();

                _ = _dbContext.Articles.Add(article);
                result[articleNumberValue] = article;
            }

            _ = await _dbContext.SaveChangesAsync(ct);
            return result;
        }

        private async Task SeedOrdersAsync(
            IReadOnlyDictionary<string, Customer> customers,
            IReadOnlyDictionary<string, Article> articles,
            CancellationToken ct)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();

            (string CustomerNumber, int MonthsAgo, int Sequence, (string ArticleNumber, int Quantity)[] Lines)[] seedOrders =
            [
                ("CU00001", 14, 1, [("ART-00001", 1), ("ART-00007", 5)]),
                ("CU00002", 11, 2, [("ART-00003", 2), ("ART-00009", 3)]),
                ("CU00003", 8, 3, [("ART-00005", 1)]),
                ("CU00004", 6, 4, [("ART-00001", 1), ("ART-00004", 1)]),
                ("CU00005", 5, 5, [("ART-00006", 1), ("ART-00010", 2)]),
                ("CU00001", 3, 6, [("ART-00009", 2)]),
                ("CU00002", 1, 7, [("ART-00002", 1)]),
                ("CU00003", 0, 8, [("ART-00003", 1), ("ART-00008", 1)]),
            ];

            foreach ((string customerNumber, int monthsAgo, int sequence, (string ArticleNumber, int Quantity)[] lines) in seedOrders)
            {
                DateTimeOffset orderInstant = now.AddMonths(-monthsAgo);
                string orderNumberValue = $"ORD-{orderInstant.Year:D4}-{sequence:D3}";
                OrderNumber orderNumber = OrderNumber.Create(orderNumberValue).EnsureValue();

                Customer customer = customers[customerNumber];

                Order? existingOrder = await _dbContext.Orders
                    .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);

                if (existingOrder is not null)
                {
                    if (existingOrder.CustomerId != customer.Id)
                    {
                        throw new InvalidOperationException(
                            $"Demo-Auftrag '{orderNumberValue}' existiert bereits mit abweichenden Daten.");
                    }

                    continue;
                }

                var deliveryDate = DateOnly.FromDateTime(orderInstant.UtcDateTime);

                CustomerAddress? customerAddress = customer.AddressAt(deliveryDate) ?? throw new InvalidOperationException(
                        $"Für Demo-Kunde '{customerNumber}' ist am {deliveryDate:yyyy-MM-dd} keine gültige Adresse hinterlegt.");

                // Billing and delivery addresses must be distinct Address instances: EF Core maps
                // each as its own owned type on Order, and cannot track one CLR instance as both.
                Address ResolvedAddress() => Address.Create(
                    customerAddress.Street,
                    customerAddress.HouseNumber,
                    customerAddress.PostalCode,
                    customerAddress.City,
                    customerAddress.CountryCode).EnsureValue();

                TimeProvider orderTimeProvider = new FixedInstantTimeProvider(orderInstant);

                Order order = Order.Create(
                    orderNumberValue,
                    customer.Id,
                    deliveryDate,
                    ResolvedAddress(),
                    AddressSource.Automatic,
                    ResolvedAddress(),
                    AddressSource.Automatic,
                    timeProvider: orderTimeProvider).EnsureValue();

                foreach ((string articleNumber, int quantity) in lines)
                {
                    Article article = articles[articleNumber];

                    // A fresh Money instance per line: EF Core owns Article.Price and each
                    // OrderLine.UnitPrice separately and cannot track one instance as both.
                    Money lineUnitPrice = Money.From(article.Price.Amount, article.Price.Currency).EnsureValue();

                    order.AddLine(article.Id, article.Name, lineUnitPrice, quantity).EnsureSuccess();
                    article.UpdateStock(-quantity).EnsureSuccess();
                }

                order.MarkInventoryApplied().EnsureSuccess();

                _ = _dbContext.Orders.Add(order);
            }

            _ = await _dbContext.SaveChangesAsync(ct);
        }

        private sealed class FixedInstantTimeProvider(DateTimeOffset instant) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => instant;
        }
    }
}
