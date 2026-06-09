using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using OrderManagement.Application.Abstractions;
using OrderManagement.Application.Abstractions.Interfaces.Catalog.Command;
using OrderManagement.Application.Abstractions.Interfaces.Catalog.Query;
using OrderManagement.Application.Abstractions.Interfaces.Customers.Command;
using OrderManagement.Application.Abstractions.Interfaces.Customers.Query;
using OrderManagement.Application.Abstractions.Interfaces.Invoices.Query;
using OrderManagement.Application.Abstractions.Interfaces.Orders.Command;
using OrderManagement.Application.Abstractions.Interfaces.Orders.Query;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Command;
using OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Query;
using OrderManagement.Infrastructure.Persistence.Repositories.Customers.Command;
using OrderManagement.Infrastructure.Persistence.Repositories.Customers.Query;
using OrderManagement.Infrastructure.Persistence.Repositories.Invoices.Query;
using OrderManagement.Infrastructure.Persistence.Repositories.Orders.Command;
using OrderManagement.Infrastructure.Persistence.Repositories.Orders.Query;

namespace OrderManagement.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddOrderManagementInfrastructure(
            this IServiceCollection services,
            string connectionString)
        {
            _ = services.AddDbContext<OrderManagementDbContext>(options =>
            {
                _ = options.UseSqlServer(connectionString);
            });

            _ = services.AddScoped<IUnitOfWork, UnitOfWork>();

            _ = services.AddScoped<ICustomerCommandRepository, CustomerCommandRepository>();
            _ = services.AddScoped<ICustomerQueryRepository, CustomerQueryRepository>();

            _ = services.AddScoped<IArticleCommandRepository, ArticleCommandRepository>();
            _ = services.AddScoped<IArticleQueryRepository, ArticleQueryRepository>();
            _ = services.AddScoped<IArticleGroupCommandRepository, ArticleGroupCommandRepository>();
            _ = services.AddScoped<IArticleGroupQueryRepository, ArticleGroupQueryRepository>();

            _ = services.AddScoped<IOrderCommandRepository, OrderCommandRepository>();
            _ = services.AddScoped<IOrderQueryRepository, OrderQueryRepository>();
            _ = services.AddScoped<IQuarterlyKpiQueryRepository, QuarterlyKpiQueryRepository>();
            _ = services.AddScoped<IInvoiceQueryRepository, InvoiceQueryRepository>();

            return services;
        }
    }
}
