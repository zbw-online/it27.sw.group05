using OrderManagement.Domain.Catalog;
using OrderManagement.Domain.Catalog.ValueObjects;

using SharedKernel.SeedWork;

namespace OrderManagement.Application.Abstractions.Interfaces.Catalog.Command
{
    public interface IArticleGroupCommandRepository : ICommandRepository<ArticleGroup, ArticleGroupId>
    {
    }
}
