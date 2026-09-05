using OrderManagement.Application.Features.Customers.DataExchange.Shared;

namespace OrderManagement.Application.Features.Customers.ValidateCustomerDataImport
{
    public sealed record ValidateCustomerDataImportQuery(CustomerDataFile File);
}
