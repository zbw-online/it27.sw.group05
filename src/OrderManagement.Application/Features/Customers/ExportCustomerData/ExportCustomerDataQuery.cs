using OrderManagement.Application.Features.Customers.DataExchange.Shared;

namespace OrderManagement.Application.Features.Customers.ExportCustomerData
{
    public sealed record ExportCustomerDataQuery(CustomerDataFormat Format, DateTime? Stichtag);
}
