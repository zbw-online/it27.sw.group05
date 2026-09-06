using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Customers.ValueObjects;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;
using OrderManagement.Infrastructure.Persistence;

using SharedKernel.Primitives;

namespace OrderManagement.Infrastructure.IntegrationTests
{
    internal static class InfrastructureTestDataFactory
    {
        private static int _customerSequence = 10000;
        private static int _articleGroupSequence = 10000;
        private static int _articleSequence = 100000;
        private static int _orderSequence = 100;

        public static async Task<Customer> CreatePersistedCustomerAsync(
            OrderManagementDbContext dbContext,
            string? customerNumber = null,
            string? email = null,
            string lastName = "Doe",
            string surName = "John",
            DateOnly? validFrom = null,
            string street = "Customer Street",
            string houseNumber = "1",
            string postalCode = "9000",
            string city = "St. Gallen",
            string countryCode = "CH")
        {
            customerNumber ??= NextCustomerNumber();
            email ??= $"customer-{Guid.NewGuid():N}@test.local";

            Result<Customer> result = Customer.Create(
                customerNr: customerNumber,
                lastName: lastName,
                surName: surName,
                email: email,
                website: null);

            Assert.IsTrue(result.IsSuccess, result.Error);

            Customer customer = result.EnsureValue();

            Result addressResult = customer.ChangeAddress(
                validFrom: validFrom ?? new DateOnly(2026, 1, 1),
                street: street,
                houseNumber: houseNumber,
                postalCode: postalCode,
                city: city,
                countryCode: countryCode);

            Assert.IsTrue(addressResult.IsSuccess, addressResult.Error);

            _ = dbContext.Customers.Add(customer);
            _ = await dbContext.SaveChangesAsync();

            Assert.IsTrue(customer.Id.IsAssigned, "The database should generate CustomerId.");
            return customer;
        }

        public static async Task<ArticleGroup> CreatePersistedArticleGroupAsync(
            OrderManagementDbContext dbContext,
            string? name = null,
            ArticleGroupId? parentGroupId = null)
        {
            name ??= $"Test Group {Interlocked.Increment(ref _articleGroupSequence)}";

            Result<ArticleGroup> result = ArticleGroup.Create(name, parentGroupId);
            Assert.IsTrue(result.IsSuccess, result.Error);

            ArticleGroup group = result.EnsureValue();

            _ = dbContext.ArticleGroups.Add(group);
            _ = await dbContext.SaveChangesAsync();

            Assert.IsTrue(group.Id.IsAssigned, "The database should generate ArticleGroupId.");
            return group;
        }

        public static async Task<Article> CreatePersistedArticleAsync(
            OrderManagementDbContext dbContext,
            ArticleGroupId? groupId = null,
            string? articleNumber = null,
            string? name = null,
            decimal priceAmount = 10.00m,
            string priceCurrency = "CHF",
            int stock = 10,
            int reorderPoint = 20,
            decimal vatRate = 7.70m,
            string? description = null,
            ArticleStatus status = ArticleStatus.Active)
        {
            ArticleGroupId effectiveGroupId = groupId ?? (await CreatePersistedArticleGroupAsync(dbContext)).Id;
            articleNumber ??= NextArticleNumber();
            name ??= $"Test Article {Guid.NewGuid():N}";

            Result<Article> result = Article.Create(
                articleNr: articleNumber,
                name: name,
                priceAmount: priceAmount,
                priceCurrency: priceCurrency,
                groupId: effectiveGroupId,
                stock: stock,
                reorderPoint: reorderPoint,
                vatRate: vatRate,
                description: description,
                status: status);

            Assert.IsTrue(result.IsSuccess, result.Error);

            Article article = result.EnsureValue();

            _ = dbContext.Articles.Add(article);
            _ = await dbContext.SaveChangesAsync();

            Assert.IsTrue(article.Id.IsAssigned, "The database should generate ArticleId.");
            return article;
        }

        public static async Task<Order> CreatePersistedOrderAsync(
            OrderManagementDbContext dbContext,
            CustomerId? customerId = null,
            string? orderNumber = null,
            DateTime? orderDate = null,
            DateOnly? deliveryDate = null)
        {
            CustomerId effectiveCustomerId = customerId ?? (await CreatePersistedCustomerAsync(dbContext)).Id;
            orderNumber ??= NextOrderNumber();

            Result<Order> result = Order.Create(
                orderNumber: orderNumber,
                customerId: effectiveCustomerId,
                deliveryDate: deliveryDate ?? DateOnly.FromDateTime(orderDate ?? DateTime.Today),
                billingAddress: CreateValidAddress(),
                billingAddressSource: AddressSource.Automatic,
                deliveryAddress: CreateValidAddress(),
                deliveryAddressSource: AddressSource.Automatic);

            Assert.IsTrue(result.IsSuccess, result.Error);

            Order order = result.EnsureValue();

            _ = dbContext.Orders.Add(order);

            if (orderDate.HasValue)
            {
                dbContext.Entry(order).Property(o => o.OrderDate).CurrentValue = orderDate.Value;
            }

            _ = await dbContext.SaveChangesAsync();

            Assert.IsTrue(order.Id.IsAssigned, "The database should generate OrderId.");
            return order;
        }

        /// <summary>
        /// Builds a persisted order with one line but does not deduct article stock or mark inventory as
        /// applied. Use this for tests that only need an order/line to exist (e.g. querying); use
        /// <see cref="CreatePersistedOrderWithAppliedInventoryAsync"/> for stock-restoration scenarios.
        /// </summary>
        public static async Task<Order> CreatePersistedOrderWithLineAsync(
            OrderManagementDbContext dbContext,
            CustomerId? customerId = null,
            Article? article = null,
            string? orderNumber = null,
            DateTime? orderDate = null,
            int quantity = 2)
        {
            Article effectiveArticle = article ?? await CreatePersistedArticleAsync(dbContext, priceAmount: 12.50m);
            Order order = await CreatePersistedOrderAsync(dbContext, customerId, orderNumber, orderDate);

            Result addLineResult = order.AddLine(
                articleId: effectiveArticle.Id,
                articleName: effectiveArticle.Name,
                unitPrice: effectiveArticle.Price,
                quantity: quantity);

            Assert.IsTrue(addLineResult.IsSuccess, addLineResult.Error);

            _ = await dbContext.SaveChangesAsync();
            return order;
        }

        /// <summary>
        /// Builds a persisted order whose inventory has already been applied: the article's stock is
        /// deducted by <paramref name="quantity"/> and the order is marked as inventory-applied, mirroring
        /// what <c>CreateOrderUseCase</c> does for a real order before it commits.
        /// </summary>
        public static async Task<Order> CreatePersistedOrderWithAppliedInventoryAsync(
            OrderManagementDbContext dbContext,
            CustomerId? customerId = null,
            Article? article = null,
            string? orderNumber = null,
            DateTime? orderDate = null,
            int quantity = 2)
        {
            Article effectiveArticle = article ?? await CreatePersistedArticleAsync(dbContext, priceAmount: 12.50m);
            Order order = await CreatePersistedOrderAsync(dbContext, customerId, orderNumber, orderDate);

            Result addLineResult = order.AddLine(
                articleId: effectiveArticle.Id,
                articleName: effectiveArticle.Name,
                unitPrice: effectiveArticle.Price,
                quantity: quantity);

            Assert.IsTrue(addLineResult.IsSuccess, addLineResult.Error);

            Result stockResult = effectiveArticle.UpdateStock(-quantity);
            Assert.IsTrue(stockResult.IsSuccess, stockResult.Error);
            _ = dbContext.Set<Article>().Update(effectiveArticle);

            Result markAppliedResult = order.MarkInventoryApplied();
            Assert.IsTrue(markAppliedResult.IsSuccess, markAppliedResult.Error);

            _ = await dbContext.SaveChangesAsync();
            return order;
        }

        public static Address CreateValidAddress()
            => Address.Create("Main St", "1", "8000", "Zurich", "CH").EnsureValue();

        public static string NextCustomerNumber()
        {
            int next = Interlocked.Increment(ref _customerSequence);
            return $"CU{next:00000}";
        }

        public static string NextArticleNumber()
        {
            int next = Interlocked.Increment(ref _articleSequence);
            return $"ART-{next:000000}";
        }

        public static string NextOrderNumber()
        {
            int next = Interlocked.Increment(ref _orderSequence);

            return next > 999 ? throw new InvalidOperationException("Order test sequence exceeded ORD-2026-999.") : $"ORD-2026-{next:000}";
        }

        public static void ClearTracker(OrderManagementDbContext dbContext)
            => dbContext.ChangeTracker.Clear();
    }
}
