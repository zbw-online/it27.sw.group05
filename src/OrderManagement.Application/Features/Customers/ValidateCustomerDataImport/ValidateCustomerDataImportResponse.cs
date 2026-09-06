using OrderManagement.Application.Features.Customers.DataExchange.Contracts;

namespace OrderManagement.Application.Features.Customers.ValidateCustomerDataImport
{
    public sealed record ValidateCustomerDataImportResponse(
        bool IsValid,
        int TotalRecordCount,
        IReadOnlyList<CustomerImportValidationIssue> Issues);
}
