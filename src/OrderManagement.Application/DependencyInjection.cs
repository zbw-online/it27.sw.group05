using Microsoft.Extensions.DependencyInjection;

using OrderManagement.Application.Features.Catalog.CreateArticle;
using OrderManagement.Application.Features.Catalog.CreateArticleGroup;
using OrderManagement.Application.Features.Catalog.DeleteArticle;
using OrderManagement.Application.Features.Catalog.DeleteArticleGroup;
using OrderManagement.Application.Features.Catalog.GetArticleForEdit;
using OrderManagement.Application.Features.Catalog.GetArticleGroupHierarchy;
using OrderManagement.Application.Features.Catalog.RenameArticleGroup;
using OrderManagement.Application.Features.Catalog.SearchArticles;
using OrderManagement.Application.Features.Catalog.UpdateArticle;
using OrderManagement.Application.Features.Catalog.UpdateArticleStock;
using OrderManagement.Application.Features.Customers.AddCustomerAddress;
using OrderManagement.Application.Features.Customers.CreateCustomer;
using OrderManagement.Application.Features.Customers.DeleteCustomer;
using OrderManagement.Application.Features.Customers.GetCustomerDetails;
using OrderManagement.Application.Features.Customers.GetCustomerForEdit;
using OrderManagement.Application.Features.Customers.SearchCustomers;
using OrderManagement.Application.Features.Customers.UpdateCustomer;

namespace OrderManagement.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddOrderManagementApplication(this IServiceCollection services)
        {
            _ = services.AddScoped<ICreateCustomerUseCase, CreateCustomerUseCase>();
            _ = services.AddScoped<ISearchCustomersUseCase, SearchCustomersUseCase>();
            _ = services.AddScoped<IGetCustomerForEditUseCase, GetCustomerForEditUseCase>();
            _ = services.AddScoped<IUpdateCustomerUseCase, UpdateCustomerUseCase>();
            _ = services.AddScoped<IDeleteCustomerUseCase, DeleteCustomerUseCase>();
            _ = services.AddScoped<IGetCustomerDetailsUseCase, GetCustomerDetailsUseCase>();
            _ = services.AddScoped<IAddCustomerAddressUseCase, AddCustomerAddressUseCase>();

            _ = services.AddScoped<ICreateArticleUseCase, CreateArticleUseCase>();
            _ = services.AddScoped<ISearchArticlesUseCase, SearchArticlesUseCase>();
            _ = services.AddScoped<IGetArticleForEditUseCase, GetArticleForEditUseCase>();
            _ = services.AddScoped<IUpdateArticleUseCase, UpdateArticleUseCase>();
            _ = services.AddScoped<IDeleteArticleUseCase, DeleteArticleUseCase>();
            _ = services.AddScoped<IUpdateArticleStockUseCase, UpdateArticleStockUseCase>();
            _ = services.AddScoped<ICreateArticleGroupUseCase, CreateArticleGroupUseCase>();
            _ = services.AddScoped<IRenameArticleGroupUseCase, RenameArticleGroupUseCase>();
            _ = services.AddScoped<IDeleteArticleGroupUseCase, DeleteArticleGroupUseCase>();
            _ = services.AddScoped<IGetArticleGroupHierarchyUseCase, GetArticleGroupHierarchyUseCase>();

            return services;
        }
    }
}
