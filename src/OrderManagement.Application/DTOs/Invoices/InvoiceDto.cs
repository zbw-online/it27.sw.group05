namespace OrderManagement.Application.DTOs.Invoices
{
    public sealed class InvoiceDto
    {
        public int Kundennummer { get; init; }
        public string Name { get; init; } = default!;
        public string Strasse { get; init; } = default!;
        public string PLZ { get; init; } = default!;
        public string Ort { get; init; } = default!;
        public string Land { get; init; } = default!;
        public DateTime Rechnungsdatum { get; init; }
        public string Rechnungsnummer { get; init; } = default!;
        public decimal RechnungsbetragNetto { get; init; }
        public decimal RechnungsbetragBrutto { get; init; }
    }
}
