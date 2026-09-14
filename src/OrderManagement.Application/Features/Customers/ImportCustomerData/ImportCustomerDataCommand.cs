using OrderManagement.Application.Features.Customers.DataExchange.Contracts;

namespace OrderManagement.Application.Features.Customers.ImportCustomerData
{
    public sealed record ImportCustomerDataCommand(CustomerDataFile File);
}
