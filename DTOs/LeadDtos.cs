using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.DTOs
{
    public class LeadDto
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public decimal ExpectedValue { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? AssignedToName { get; set; }
    }

    public class CreateLeadDto
    {
        [Required(ErrorMessage = "Lead Name is required.")]
        [StringLength(100)]
        public string LeadName { get; set; } = string.Empty;

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

        [Required(ErrorMessage = "Source is required.")]
        public string Source { get; set; } = "Website";

        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = "New";

        public string Priority { get; set; } = "Medium";

        [Range(0, 100000000)]
        public decimal ExpectedValue { get; set; }

        public string? AssignedToId { get; set; }
    }

    public class UpdateLeadDto : CreateLeadDto
    {
    }
}
