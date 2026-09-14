using System.Xml.Serialization;

namespace OrderManagement.Infrastructure.Serialization.Customers.Xml
{
    [XmlRoot("Kunden")]
    public sealed class XmlCustomerDataDocument
    {
        [XmlElement("Kunde")]
        public List<XmlCustomerDataEntry> Kunde { get; set; } = [];
    }

    public sealed class XmlCustomerDataEntry
    {
        [XmlAttribute("CustomerNumber")]
        public string CustomerNumber { get; set; } = string.Empty;

        [XmlElement("LastName")]
        public string LastName { get; set; } = string.Empty;

        [XmlElement("SurName")]
        public string SurName { get; set; } = string.Empty;

        [XmlElement("Email")]
        public string Email { get; set; } = string.Empty;

        [XmlElement("Website")]
        public string? Website { get; set; }

        [XmlElement("Address")]
        public XmlCustomerAddressEntry? Address { get; set; }
    }

    public sealed class XmlCustomerAddressEntry
    {
        [XmlElement("ValidFrom")]
        public string ValidFrom { get; set; } = string.Empty;

        [XmlElement("Street")]
        public string Street { get; set; } = string.Empty;

        [XmlElement("HouseNumber")]
        public string HouseNumber { get; set; } = string.Empty;

        [XmlElement("PostalCode")]
        public string PostalCode { get; set; } = string.Empty;

        [XmlElement("City")]
        public string City { get; set; } = string.Empty;

        [XmlElement("CountryCode")]
        public string CountryCode { get; set; } = string.Empty;
    }
}
