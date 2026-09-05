using OrderManagement.Application.Features.Customers.DataExchange.Shared;

namespace OrderManagement.Application.Features.Customers.ValidateCustomerDataImport
{
    public sealed record ValidateCustomerDataImportResponse(
        bool IsValid,
        int TotalRecordCount,
        IReadOnlyList<CustomerImportValidationIssue> Issues);
}
