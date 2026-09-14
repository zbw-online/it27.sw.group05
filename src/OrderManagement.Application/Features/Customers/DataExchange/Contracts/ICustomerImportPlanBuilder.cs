namespace OrderManagement.Application.Features.Customers.DataExchange.Contracts
{
    public interface ICustomerImportPlanBuilder
    {
        Task<CustomerImportPlan> BuildAsync(CustomerDataFile file, CancellationToken cancellationToken = default);
    }
}
