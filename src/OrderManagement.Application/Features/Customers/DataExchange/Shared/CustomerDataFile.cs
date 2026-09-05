namespace OrderManagement.Application.Features.Customers.DataExchange.Shared
{
    public sealed record CustomerDataFile(
        string SafeFileName,
        CustomerDataFormat Format,
        string MediaType,
        byte[] Content);
}
