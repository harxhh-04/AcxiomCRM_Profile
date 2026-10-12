using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class Activity
    {
        [Key]
        public int ActivityId { get; set; }

        [Required(ErrorMessage = "Activity type is required.")]
        [StringLength(30)]
        [Display(Name = "Activity Type")]
        public string ActivityType { get; set; } = "Call"; // Call, Meeting, Email, Task

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(150, ErrorMessage = "Subject cannot exceed 150 characters.")]
        public string Subject { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Activity Date")]
        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Customer")]
        public int? CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [Display(Name = "Lead")]
        public int? LeadId { get; set; }

        [ForeignKey("LeadId")]
        public virtual Lead? Lead { get; set; }

        [Display(Name = "Opportunity")]
        public int? OpportunityId { get; set; }

        [ForeignKey("OpportunityId")]
        public virtual Opportunity? Opportunity { get; set; }

        [Display(Name = "Assigned To")]
        public string? AssignedToId { get; set; }

        [ForeignKey("AssignedToId")]
        public virtual ApplicationUser? AssignedTo { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Completed"; // Completed, Pending, Cancelled
    }
}
