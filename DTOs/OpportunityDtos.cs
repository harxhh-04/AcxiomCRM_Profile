using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.DTOs
{
    public class OpportunityDto
    {
        public int OpportunityId { get; set; }
        public string OpportunityName { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? LeadId { get; set; }
        public decimal Amount { get; set; }
        public string Stage { get; set; } = string.Empty;
        public int Probability { get; set; }
        public decimal WeightedValue { get; set; }
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? AssignedToName { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateOpportunityDto
    {
        [Required(ErrorMessage = "Opportunity Name is required.")]
        [StringLength(150)]
        public string OpportunityName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer is required.")]
        public int CustomerId { get; set; }

        public int? LeadId { get; set; }

        [Required]
        [Range(0.01, 100000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required]
        public string Stage { get; set; } = "Qualification";

        [Required]
        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 10;

        [Required]
        public DateTime ExpectedCloseDate { get; set; }

        public string? AssignedToId { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateOpportunityDto : CreateOpportunityDto
    {
        public string Status { get; set; } = "Open";
    }

    public class PipelineReportDto
    {
        public decimal TotalPipelineValue { get; set; }
        public decimal TotalWeightedValue { get; set; }
        public int TotalOpenOpportunities { get; set; }
        public Dictionary<string, decimal> StageBreakdown { get; set; } = new();
    }
}
