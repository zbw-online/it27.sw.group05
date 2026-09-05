namespace OrderManagement.Application.Features.Customers.DataExchange.Shared
{
    public interface ICustomerImportPlanBuilder
    {
        Task<CustomerImportPlan> BuildAsync(CustomerDataFile file, CancellationToken cancellationToken = default);
    }
}
