using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Application.Abstractions.Persistence.Catalog.Command;
using OrderManagement.Application.Abstractions.Persistence.Catalog.Query;
using OrderManagement.Application.Abstractions.Persistence.Customers.Command;
using OrderManagement.Application.Abstractions.Persistence.Customers.Query;
using OrderManagement.Application.Abstractions.Persistence.Invoices.Query;
using OrderManagement.Application.Abstractions.Persistence.Orders.Command;
using OrderManagement.Application.Abstractions.Persistence.Orders.Query;
using OrderManagement.Application.Abstractions.Serialization;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Command;
using OrderManagement.Infrastructure.Persistence.Repositories.Catalog.Query;
using OrderManagement.Infrastructure.Persistence.Repositories.Customers.Command;
using OrderManagement.Infrastructure.Persistence.Repositories.Customers.Query;
using OrderManagement.Infrastructure.Persistence.Repositories.Invoices.Query;
using OrderManagement.Infrastructure.Persistence.Repositories.Orders.Command;
using OrderManagement.Infrastructure.Persistence.Repositories.Orders.Query;
using OrderManagement.Infrastructure.Serialization.Customers;

namespace OrderManagement.Infrastructure
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddOrderManagementInfrastructure(
            this IServiceCollection services,
            string connectionString,
            bool enableDetailedErrors = false)
        {
            _ = services.AddPersistence(connectionString, enableDetailedErrors);
            _ = services.AddCustomerDataSerialization();

            return services;
        }

        private static IServiceCollection AddPersistence(
            this IServiceCollection services,
            string connectionString,
            bool enableDetailedErrors)
        {
            _ = services.AddDbContext<OrderManagementDbContext>(options =>
            {
                _ = options.UseSqlServer(connectionString);

                if (enableDetailedErrors)
                {
                    _ = options.EnableDetailedErrors().EnableSensitiveDataLogging();
                }
            });

            _ = services.AddScoped<IUnitOfWork, UnitOfWork>();

            _ = services.AddScoped<ICustomerCommandRepository, CustomerCommandRepository>();
            _ = services.AddScoped<ICustomerQueryRepository, CustomerQueryRepository>();
            _ = services.AddScoped<ICustomerTemporalQueryRepository, CustomerTemporalQueryRepository>();

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

        private static IServiceCollection AddCustomerDataSerialization(this IServiceCollection services)
        {
            _ = services.AddSingleton<ICustomerDataSerializer, JsonCustomerDataSerializer>();
            _ = services.AddSingleton<ICustomerDataSerializer, XmlCustomerDataSerializer>();
            _ = services.AddSingleton<ICustomerDataSerializerResolver, CustomerDataSerializerResolver>();

            return services;
        }
    }
}
