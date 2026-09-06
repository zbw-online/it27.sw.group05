using Microsoft.Extensions.DependencyInjection;

using OrderManagement.Application.Features.Catalog.CreateArticle;
using OrderManagement.Application.Features.Catalog.CreateArticleGroup;
using OrderManagement.Application.Features.Catalog.DeactivateArticle;
using OrderManagement.Application.Features.Catalog.DeleteArticle;
using OrderManagement.Application.Features.Catalog.DeleteArticleGroup;
using OrderManagement.Application.Features.Catalog.GetArticleForEdit;
using OrderManagement.Application.Features.Catalog.GetArticleGroupForEdit;
using OrderManagement.Application.Features.Catalog.GetArticleGroupHierarchy;
using OrderManagement.Application.Features.Catalog.GetLowStockArticles;
using OrderManagement.Application.Features.Catalog.ReactivateArticle;
using OrderManagement.Application.Features.Catalog.ReconcileInventory;
using OrderManagement.Application.Features.Catalog.RenameArticleGroup;
using OrderManagement.Application.Features.Catalog.SearchArticleGroups;
using OrderManagement.Application.Features.Catalog.SearchArticles;
using OrderManagement.Application.Features.Catalog.UpdateArticle;
using OrderManagement.Application.Features.Catalog.UpdateArticleStock;
using OrderManagement.Application.Features.Customers.AddCustomerAddress;
using OrderManagement.Application.Features.Customers.CreateCustomer;
using OrderManagement.Application.Features.Customers.DataExchange.Contracts;
using OrderManagement.Application.Features.Customers.DeleteCustomer;
using OrderManagement.Application.Features.Customers.ExportCustomerData;
using OrderManagement.Application.Features.Customers.GetCustomerDetails;
using OrderManagement.Application.Features.Customers.GetCustomerForEdit;
using OrderManagement.Application.Features.Customers.GetCustomersWithoutCurrentAddress;
using OrderManagement.Application.Features.Customers.ImportCustomerData;
using OrderManagement.Application.Features.Customers.PreviewAddressForDate;
using OrderManagement.Application.Features.Customers.SearchCustomers;
using OrderManagement.Application.Features.Customers.UpdateCustomer;
using OrderManagement.Application.Features.Customers.ValidateCustomerDataImport;
using OrderManagement.Application.Features.Orders.AddOrderLine;
using OrderManagement.Application.Features.Orders.CreateOrder;
using OrderManagement.Application.Features.Orders.DeleteOrder;
using OrderManagement.Application.Features.Orders.GetDashboardOverview;
using OrderManagement.Application.Features.Orders.GetNextOrderNumber;
using OrderManagement.Application.Features.Orders.GetOrderDetails;
using OrderManagement.Application.Features.Orders.GetQuarterlyKpis;
using OrderManagement.Application.Features.Orders.GetTopSellingArticles;
using OrderManagement.Application.Features.Orders.RemoveOrderLine;
using OrderManagement.Application.Features.Orders.SearchOrders;
using OrderManagement.Application.Features.Orders.UpdateOrderLineQuantity;

namespace OrderManagement.Application
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddOrderManagementApplication(this IServiceCollection services)
        {
            _ = services.AddSingleton(TimeProvider.System);
            _ = services.AddOptions<CustomerDataExchangeOptions>();

            _ = services.AddScoped<ICreateCustomerUseCase, CreateCustomerUseCase>();
            _ = services.AddScoped<ISearchCustomersUseCase, SearchCustomersUseCase>();
            _ = services.AddScoped<IGetCustomerForEditUseCase, GetCustomerForEditUseCase>();
            _ = services.AddScoped<IUpdateCustomerUseCase, UpdateCustomerUseCase>();
            _ = services.AddScoped<IDeleteCustomerUseCase, DeleteCustomerUseCase>();
            _ = services.AddScoped<IGetCustomerDetailsUseCase, GetCustomerDetailsUseCase>();
            _ = services.AddScoped<IAddCustomerAddressUseCase, AddCustomerAddressUseCase>();
            _ = services.AddScoped<IGetCustomersWithoutCurrentAddressUseCase, GetCustomersWithoutCurrentAddressUseCase>();
            _ = services.AddScoped<IPreviewAddressForDateUseCase, PreviewAddressForDateUseCase>();
            _ = services.AddScoped<ICustomerImportPlanBuilder, CustomerImportPlanBuilder>();
            _ = services.AddScoped<IValidateCustomerDataImportUseCase, ValidateCustomerDataImportUseCase>();
            _ = services.AddScoped<IImportCustomerDataUseCase, ImportCustomerDataUseCase>();
            _ = services.AddScoped<IExportCustomerDataUseCase, ExportCustomerDataUseCase>();

            _ = services.AddScoped<ICreateArticleUseCase, CreateArticleUseCase>();
            _ = services.AddScoped<ISearchArticlesUseCase, SearchArticlesUseCase>();
            _ = services.AddScoped<IGetArticleForEditUseCase, GetArticleForEditUseCase>();
            _ = services.AddScoped<IGetLowStockArticlesUseCase, GetLowStockArticlesUseCase>();
            _ = services.AddScoped<IUpdateArticleUseCase, UpdateArticleUseCase>();
            _ = services.AddScoped<IDeleteArticleUseCase, DeleteArticleUseCase>();
            _ = services.AddScoped<IDeactivateArticleUseCase, DeactivateArticleUseCase>();
            _ = services.AddScoped<IReactivateArticleUseCase, ReactivateArticleUseCase>();
            _ = services.AddScoped<IReconcileInventoryUseCase, ReconcileInventoryUseCase>();
            _ = services.AddScoped<IUpdateArticleStockUseCase, UpdateArticleStockUseCase>();
            _ = services.AddScoped<ICreateArticleGroupUseCase, CreateArticleGroupUseCase>();
            _ = services.AddScoped<ISearchArticleGroupsUseCase, SearchArticleGroupsUseCase>();
            _ = services.AddScoped<IGetArticleGroupForEditUseCase, GetArticleGroupForEditUseCase>();
            _ = services.AddScoped<IRenameArticleGroupUseCase, RenameArticleGroupUseCase>();
            _ = services.AddScoped<IDeleteArticleGroupUseCase, DeleteArticleGroupUseCase>();
            _ = services.AddScoped<IGetArticleGroupHierarchyUseCase, GetArticleGroupHierarchyUseCase>();

            _ = services.AddScoped<ICreateOrderUseCase, CreateOrderUseCase>();
            _ = services.AddScoped<ISearchOrdersUseCase, SearchOrdersUseCase>();
            _ = services.AddScoped<IGetOrderDetailsUseCase, GetOrderDetailsUseCase>();
            _ = services.AddScoped<IAddOrderLineUseCase, AddOrderLineUseCase>();
            _ = services.AddScoped<IUpdateOrderLineQuantityUseCase, UpdateOrderLineQuantityUseCase>();
            _ = services.AddScoped<IRemoveOrderLineUseCase, RemoveOrderLineUseCase>();
            _ = services.AddScoped<IDeleteOrderUseCase, DeleteOrderUseCase>();
            _ = services.AddScoped<IGetDashboardOverviewUseCase, GetDashboardOverviewUseCase>();
            _ = services.AddScoped<IGetQuarterlyKpisUseCase, GetQuarterlyKpisUseCase>();
            _ = services.AddScoped<IGetTopSellingArticlesUseCase, GetTopSellingArticlesUseCase>();
            _ = services.AddScoped<IGetNextOrderNumberUseCase, GetNextOrderNumberUseCase>();

            return services;
        }
    }
}
