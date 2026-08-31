using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Customers;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.PlaywrightTests.Support
{
    internal static class PlaywrightSeedData
    {
        internal const string CustomerWithFutureMoveNumber = "CU00001";
        internal const string ReferencedArticleNumber = "ART-REF-001";
        internal const string RootCategoryName = "Elektronik";
        internal const string LeafCategoryName = "USB-C Kabel";

        internal static readonly DateOnly OldAddressValidTo = new(2026, 8, 31);
        internal static readonly DateOnly NewAddressValidFrom = new(2026, 9, 1);

        internal static async Task SeedAsync(OrderManagementDbContext dbContext)
        {
            Customer customer = Customer.Create(
                CustomerWithFutureMoveNumber, "Muster", "Maria", "maria.muster@example.com", null).EnsureValue();
            customer.ChangeAddress(new DateOnly(2026, 1, 1), "Alte Gasse", "1", "9000", "St. Gallen", "CH").EnsureSuccess();
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
        }
    }
}
