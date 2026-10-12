using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class FollowUp
    {
        [Key]
        public int FollowUpId { get; set; }

        [Display(Name = "Related Customer")]
        public int? CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [Display(Name = "Related Lead")]
        public int? LeadId { get; set; }

        [ForeignKey("LeadId")]
        public virtual Lead? Lead { get; set; }

        [Display(Name = "Related Opportunity")]
        public int? OpportunityId { get; set; }

        [ForeignKey("OpportunityId")]
        public virtual Opportunity? Opportunity { get; set; }

        [Required(ErrorMessage = "Follow-up date is required.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Follow-up Date & Time")]
        public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.AddDays(1);

        [Required(ErrorMessage = "Follow-up Type is required.")]
        [StringLength(30)]
        [Display(Name = "Type")]
        public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Demo, Visit

        [Required(ErrorMessage = "Subject is required.")]
        [StringLength(150, ErrorMessage = "Subject cannot exceed 150 characters.")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Remarks are required.")]
        [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters.")]
        public string Remarks { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

        [Display(Name = "Assigned User")]
        public string? AssignedToId { get; set; }

        [ForeignKey("AssignedToId")]
        public virtual ApplicationUser? AssignedTo { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public bool IsOverdue => Status == "Planned" && FollowUpDate < DateTime.UtcNow;
    }
}
