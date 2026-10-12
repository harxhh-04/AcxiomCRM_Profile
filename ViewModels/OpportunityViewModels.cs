using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels
{
    public class OpportunityIndexViewModel
    {
        public List<Opportunity> Opportunities { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? StageFilter { get; set; }
        public string? StatusFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public decimal TotalPipelineAmount { get; set; }
        public decimal TotalWeightedAmount { get; set; }
    }

    public class OpportunityDetailsViewModel
    {
        public Opportunity Opportunity { get; set; } = null!;
        public List<FollowUp> FollowUps { get; set; } = new();
        public List<Activity> Activities { get; set; } = new();
    }
}
