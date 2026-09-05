using OrderManagement.Application.Features.Customers.DataExchange.Shared;

namespace OrderManagement.Application.Features.Customers.ImportCustomerData
{
    public sealed record ImportCustomerDataResponse(
        bool IsValid,
        int ImportedCount,
        int TotalRecordCount,
        IReadOnlyList<CustomerImportValidationIssue> Issues);
}
