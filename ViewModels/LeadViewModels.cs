using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels
{
    public class LeadIndexViewModel
    {
        public List<Lead> Leads { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public string? AssignedFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public List<SelectListItem> UsersList { get; set; } = new();
    }

    public class LeadDetailsViewModel
    {
        public Lead Lead { get; set; } = null!;
        public List<FollowUp> FollowUps { get; set; } = new();
        public List<Activity> Activities { get; set; } = new();
    }

    public class ConvertLeadViewModel
    {
        public int LeadId { get; set; }

        [Display(Name = "Lead Name")]
        public string LeadName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }

        [Display(Name = "Create Sales Opportunity on Conversion?")]
        public bool CreateOpportunity { get; set; } = true;

        [StringLength(150)]
        [Display(Name = "Opportunity Name")]
        public string? OpportunityName { get; set; }

        [Range(0.01, 100000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Display(Name = "Deal Amount ($)")]
        public decimal? Amount { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Expected Close Date")]
        public DateTime? ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);
    }
}
