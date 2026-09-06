using OrderManagement.Application.Features.Customers.DataExchange.Contracts;

namespace OrderManagement.Application.Features.Customers.ExportCustomerData
{
    public sealed record ExportCustomerDataQuery(CustomerDataFormat Format, DateTime? Stichtag);
}
