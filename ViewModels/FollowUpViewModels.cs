using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels
{
    public class FollowUpIndexViewModel
    {
        public List<FollowUp> FollowUps { get; set; } = new();
        public string? StatusFilter { get; set; }
        public string? TypeFilter { get; set; }
        public string? DateFilter { get; set; } // all, upcoming, overdue, today
        public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
        public int UpcomingCount { get; set; }
        public int OverdueCount { get; set; }
        public int CompletedCount { get; set; }
    }
}
