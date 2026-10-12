using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.DTOs
{
    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedDate { get; set; }
        public string? AssignedToName { get; set; }
    }

    public class CreateCustomerDto
    {
        [Required(ErrorMessage = "Customer Name is required.")]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [RegularExpression(@"^[0-9+\-\s()]{10,15}$", ErrorMessage = "Enter a valid phone number (10-15 digits).")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        public string? CompanyName { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? State { get; set; }

        public string? AssignedToId { get; set; }
    }

    public class UpdateCustomerDto : CreateCustomerDto
    {
        [Required]
        public string Status { get; set; } = "Active";
    }
}
