using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Presentation.Blazor.Features.Customers.CreateCustomer
{
    public sealed class CreateCustomerFormModel
    {
        [Required]
        public string CustomerNumber { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string SurName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? Website { get; set; }

        [Required]
        public DateTime AddressValidFrom { get; set; } = DateTime.Today;

        [Required]
        public string Street { get; set; } = string.Empty;

        [Required]
        public string HouseNumber { get; set; } = string.Empty;

        [Required]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string CountryCode { get; set; } = "CH";
    }
}
