using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class Lead
    {
        [Key]
        public int LeadId { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Lead Code")]
        public string LeadCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lead Name is required.")]
        [StringLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters.")]
        [Display(Name = "Lead Name")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [RegularExpression(@"^[0-9+\-\s()]{10,15}$", ErrorMessage = "Enter a valid phone number (10-15 digits).")]
        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Lead Source is required.")]
        [StringLength(50)]
        public string Source { get; set; } = "Website"; // Website, Referral, Cold Call, Partner, Event, Social Media

        [Required(ErrorMessage = "Lead Status is required.")]
        [StringLength(30)]
        public string Status { get; set; } = "New"; // New, Contacted, Qualified, Unqualified, Converted, Lost

        [StringLength(20)]
        public string Priority { get; set; } = "Medium"; // Low, Medium, High

        [Range(0, 100000000, ErrorMessage = "Expected value must be greater than or equal to 0.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Expected Value ($)")]
        public decimal ExpectedValue { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Assigned To")]
        public string? AssignedToId { get; set; }

        [ForeignKey("AssignedToId")]
        public virtual ApplicationUser? AssignedTo { get; set; }

        // Conversion tracking
        public int? ConvertedCustomerId { get; set; }

        [ForeignKey("ConvertedCustomerId")]
        public virtual Customer? ConvertedCustomer { get; set; }

        public int? ConvertedOpportunityId { get; set; }

        [ForeignKey("ConvertedOpportunityId")]
        public virtual Opportunity? ConvertedOpportunity { get; set; }

        // Navigation collections
        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
