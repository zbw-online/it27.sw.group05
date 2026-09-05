using OrderManagement.Application.Features.Customers.DataExchange.Shared;

namespace OrderManagement.Application.Features.Customers.ImportCustomerData
{
    public sealed record ImportCustomerDataCommand(CustomerDataFile File);
}
