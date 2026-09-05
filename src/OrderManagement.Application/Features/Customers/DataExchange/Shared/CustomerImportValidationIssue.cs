namespace OrderManagement.Application.Features.Customers.DataExchange.Shared
{
    public sealed record CustomerImportValidationIssue(
        int? RecordIndex,
        string? CustomerNumber,
        string Field,
        string Message);
}
