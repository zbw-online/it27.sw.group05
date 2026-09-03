using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Orders.ValueObjects;
using OrderManagement.Infrastructure.Persistence;

using SharedKernel.Primitives;

namespace OrderManagement.PlaywrightTests.Support
{
    internal static class PlaywrightSeedData
    {
        internal const string CustomerWithFutureMoveNumber = "CU00001";
        internal const string ReferencedArticleNumber = "ART-REF-001";
        internal const string RootCategoryName = "Elektronik";
        internal const string LeafCategoryName = "USB-C Kabel";

        internal const string DeletableOrderNumber = "ORD-2026-901";
        internal const string DeletableOrderCustomerNumber = "CU00002";
        internal const int DeletableOrderDeductedQuantity = 5;
        internal const int DeletableOrderArticleStockBeforeDeletion = 20;
        internal const int DeletableOrderArticleStockAfterDeletion = 25;

        // Never touched by any test: keeps the orders list non-empty after DeletableOrderNumber
        // is deleted, so ".data-table" still renders instead of the empty state.
        internal const string AnchorOrderNumber = "ORD-2026-900";
        internal const string AnchorOrderCustomerNumber = "CU00003";

        // The application clock is pinned to this instant (see PlaywrightAppFixture), so address
        // classification in GetCustomerDetailsUseCase stays deterministic regardless of the real calendar date.
        internal static readonly DateOnly ReferenceDate = new(2026, 6, 15);
        internal static readonly DateTimeOffset ReferenceNow = new(ReferenceDate.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);

        internal static readonly DateOnly CurrentAddressValidFrom = ReferenceDate.AddMonths(-6);
        internal static readonly DateOnly OldAddressValidTo = ReferenceDate;
        internal static readonly DateOnly NewAddressValidFrom = ReferenceDate.AddDays(1);

        internal static async Task SeedAsync(OrderManagementDbContext dbContext)
        {
            Customer customer = Customer.Create(
                CustomerWithFutureMoveNumber, "Muster", "Maria", "maria.muster@example.com", null).EnsureValue();
            customer.ChangeAddress(CurrentAddressValidFrom, "Alte Gasse", "1", "9000", "St. Gallen", "CH").EnsureSuccess();
            customer.ChangeAddress(NewAddressValidFrom, "Neue Gasse", "2", "8000", "Zürich", "CH").EnsureSuccess();
            _ = dbContext.Customers.Add(customer);

            ArticleGroup root = ArticleGroup.Create(RootCategoryName).EnsureValue();
            _ = dbContext.ArticleGroups.Add(root);
            _ = await dbContext.SaveChangesAsync();

            ArticleGroup level2 = ArticleGroup.Create("Kabel & Adapter", root.Id).EnsureValue();
            _ = dbContext.ArticleGroups.Add(level2);
            _ = await dbContext.SaveChangesAsync();

            ArticleGroup level3 = ArticleGroup.Create("USB", level2.Id).EnsureValue();
            _ = dbContext.ArticleGroups.Add(level3);
            _ = await dbContext.SaveChangesAsync();

            ArticleGroup level4 = ArticleGroup.Create("USB-C", level3.Id).EnsureValue();
            _ = dbContext.ArticleGroups.Add(level4);
            _ = await dbContext.SaveChangesAsync();

            Article referencedArticle = Article.Create(
                ReferencedArticleNumber, LeafCategoryName, 12.50m, "CHF", level4.Id, stock: 25).EnsureValue();
            _ = dbContext.Articles.Add(referencedArticle);

            Article inactiveCandidateArticle = Article.Create(
                "ART-REF-002", "Ladekabel 2m", 9.90m, "CHF", level4.Id, stock: 40).EnsureValue();
            _ = dbContext.Articles.Add(inactiveCandidateArticle);

            _ = await dbContext.SaveChangesAsync();

            Customer orderCustomer = Customer.Create(
                DeletableOrderCustomerNumber, "Muster", "Max", "max.muster@example.com", null).EnsureValue();
            orderCustomer.ChangeAddress(new DateOnly(2026, 1, 1), "Bahnhofstrasse", "1", "8000", "Zürich", "CH").EnsureSuccess();
            _ = dbContext.Customers.Add(orderCustomer);
            _ = await dbContext.SaveChangesAsync();

            Order deletableOrder = Order.Create(
                DeletableOrderNumber,
                orderCustomer.Id,
                new DateOnly(2026, 9, 10),
                Address.Create("Bahnhofstrasse", "1", "8000", "Zürich", "CH").EnsureValue(),
                AddressSource.Automatic,
                Address.Create("Bahnhofstrasse", "1", "8000", "Zürich", "CH").EnsureValue(),
                AddressSource.Automatic).EnsureValue();
            deletableOrder.AddLine(referencedArticle.Id, referencedArticle.Name, referencedArticle.Price, DeletableOrderDeductedQuantity).EnsureSuccess();
            referencedArticle.UpdateStock(-DeletableOrderDeductedQuantity).EnsureSuccess();
            deletableOrder.MarkInventoryApplied().EnsureSuccess();
            _ = dbContext.Orders.Add(deletableOrder);

            Customer anchorCustomer = Customer.Create(
                AnchorOrderCustomerNumber, "Muster", "Anna", "anna.muster@example.com", null).EnsureValue();
            anchorCustomer.ChangeAddress(new DateOnly(2026, 1, 1), "Seestrasse", "1", "8001", "Zürich", "CH").EnsureSuccess();
            _ = dbContext.Customers.Add(anchorCustomer);
            _ = await dbContext.SaveChangesAsync();

            Order anchorOrder = Order.Create(
                AnchorOrderNumber,
                anchorCustomer.Id,
                new DateOnly(2026, 9, 10),
                Address.Create("Seestrasse", "1", "8001", "Zürich", "CH").EnsureValue(),
                AddressSource.Automatic,
                Address.Create("Seestrasse", "1", "8001", "Zürich", "CH").EnsureValue(),
                AddressSource.Automatic).EnsureValue();
            anchorOrder.AddLine(inactiveCandidateArticle.Id, inactiveCandidateArticle.Name, inactiveCandidateArticle.Price, 1).EnsureSuccess();
            inactiveCandidateArticle.UpdateStock(-1).EnsureSuccess();
            anchorOrder.MarkInventoryApplied().EnsureSuccess();
            _ = dbContext.Orders.Add(anchorOrder);

            _ = await dbContext.SaveChangesAsync();
        }
    }
}
