using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels
{
    public class ReportFilterViewModel
    {
        public string ReportType { get; set; } = "pipeline"; // pipeline, conversion, customer, lead, followup, activity
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? AssignedToId { get; set; }
    }

    public class PipelineReportItem
    {
        public string Stage { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
    }

    public class OwnerPipelineReportItem
    {
        public string OwnerName { get; set; } = string.Empty;
        public int OpportunityCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
    }

    public class ConversionReportViewModel
    {
        public int TotalLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public int LostLeads { get; set; }
        public int ActiveLeads { get; set; }
        public double ConversionRate => TotalLeads > 0 ? Math.Round((double)ConvertedLeads / TotalLeads * 100, 1) : 0;
        public decimal TotalConvertedOpportunityValue { get; set; }
    }

    public class UserActivityReportItem
    {
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int TotalActions { get; set; }
        public int CreatedRecords { get; set; }
        public int UpdatedRecords { get; set; }
        public int FollowUpsLogged { get; set; }
        public DateTime? LastActive { get; set; }
    }
}
