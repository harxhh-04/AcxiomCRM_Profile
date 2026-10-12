using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class Opportunity
    {
        [Key]
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Opportunity Name is required.")]
        [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
        [Display(Name = "Opportunity Name")]
        public string OpportunityName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer is required.")]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [Display(Name = "Originating Lead")]
        public int? LeadId { get; set; }

        [ForeignKey("LeadId")]
        public virtual Lead? Lead { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, 100000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Amount ($)")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Stage is required.")]
        [StringLength(30)]
        public string Stage { get; set; } = "Qualification"; // Qualification, Proposal, Negotiation, Won, Lost

        [Required(ErrorMessage = "Probability is required.")]
        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 10;

        [Required(ErrorMessage = "Expected Close Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Expected Close Date")]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

        [StringLength(20)]
        public string Status { get; set; } = "Open"; // Open, Won, Lost

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Assigned To")]
        public string? AssignedToId { get; set; }

        [ForeignKey("AssignedToId")]
        public virtual ApplicationUser? AssignedTo { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        [NotMapped]
        [Display(Name = "Weighted Value ($)")]
        public decimal WeightedValue => (Amount * Probability) / 100m;

        // Navigation collections
        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
