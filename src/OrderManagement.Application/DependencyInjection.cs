using Microsoft.Extensions.DependencyInjection;

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

            return services;
        }
    }
}
