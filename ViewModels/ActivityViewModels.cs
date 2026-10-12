using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels
{
    public class ActivityIndexViewModel
    {
        public List<Activity> Activities { get; set; } = new();
        public string? TypeFilter { get; set; }
        public string? StatusFilter { get; set; }
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }
    }
}
