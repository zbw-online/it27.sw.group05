using OrderManagement.Application.Features.Customers.DataExchange.Contracts;

namespace OrderManagement.Application.Features.Customers.ExportCustomerData
{
    public sealed record ExportCustomerDataQuery(CustomerDataFormat Format, CustomerExportMode Mode, DateTime? Stichtag)
    {
        public static ExportCustomerDataQuery Current(CustomerDataFormat format) => new(format, CustomerExportMode.Current, null);

        public static ExportCustomerDataQuery Historical(CustomerDataFormat format, DateTime stichtag) => new(format, CustomerExportMode.Historical, stichtag);
    }
}
