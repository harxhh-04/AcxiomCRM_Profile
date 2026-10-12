using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ICrmService _crmService;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ICrmService crmService, UserManager<ApplicationUser> userManager)
        {
            _crmService = crmService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string dateFilter = "all")
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            var primaryRole = roles.FirstOrDefault() ?? "SalesExecutive";

            var customersQuery = _crmService.GetScopedCustomers(User);
            var leadsQuery = _crmService.GetScopedLeads(User);
            var oppsQuery = _crmService.GetScopedOpportunities(User);
            var followUpsQuery = _crmService.GetScopedFollowUps(User);

            // Apply date filters if specified
            DateTime? filterStartDate = null;
            var now = DateTime.UtcNow;

            switch (dateFilter.ToLower())
            {
                case "today":
                    filterStartDate = now.Date;
                    break;
                case "week":
                    filterStartDate = now.AddDays(-(int)now.DayOfWeek);
                    break;
                case "month":
                    filterStartDate = new DateTime(now.Year, now.Month, 1);
                    break;
                default:
                    dateFilter = "all";
                    break;
            }

            if (filterStartDate.HasValue)
            {
                customersQuery = customersQuery.Where(c => c.CreatedDate >= filterStartDate.Value);
                leadsQuery = leadsQuery.Where(l => l.CreatedDate >= filterStartDate.Value);
                oppsQuery = oppsQuery.Where(o => o.CreatedDate >= filterStartDate.Value);
            }

            var model = new DashboardViewModel
            {
                UserRole = primaryRole,
                UserName = user?.FullName ?? User.Identity?.Name ?? "User",
                DateFilter = dateFilter,

                TotalCustomers = await customersQuery.CountAsync(),
                TotalLeads = await leadsQuery.CountAsync(),
                OpenLeads = await leadsQuery.CountAsync(l => l.Status != "Lost" && l.Status != "Converted"),
                TotalOpportunities = await oppsQuery.CountAsync(),
                OpenOpportunities = await oppsQuery.CountAsync(o => o.Status == "Open"),
                WonOpportunities = await oppsQuery.CountAsync(o => o.Status == "Won"),
                LostOpportunities = await oppsQuery.CountAsync(o => o.Status == "Lost"),
                TotalPipelineValue = await oppsQuery.Where(o => o.Status == "Open").SumAsync(o => (decimal?)o.Amount) ?? 0m
            };

            // 1. Chart: Lead Status Breakdown
            var leadStatuses = new[] { "New", "Contacted", "Qualified", "Converted", "Lost" };
            model.LeadStatusLabels = leadStatuses.ToList();
            var leadGroupCounts = await leadsQuery
                .GroupBy(l => l.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            model.LeadStatusCounts = leadStatuses
                .Select(status => leadGroupCounts.FirstOrDefault(g => g.Status == status)?.Count ?? 0)
                .ToList();

            // 2. Chart: Opportunity Pipeline by Stage
            var pipelineStages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
            model.PipelineStageLabels = pipelineStages.ToList();
            var oppStageGroups = await oppsQuery
                .GroupBy(o => o.Stage)
                .Select(g => new { Stage = g.Key, Total = g.Sum(x => x.Amount) })
                .ToListAsync();

            model.PipelineStageAmounts = pipelineStages
                .Select(stage => oppStageGroups.FirstOrDefault(g => g.Stage == stage)?.Total ?? 0m)
                .ToList();

            // 3. Chart: Monthly Sales (last 6 months)
            var monthLabels = new List<string>();
            var monthTotals = new List<decimal>();
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = now.AddMonths(-i);
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                monthLabels.Add(monthStart.ToString("MMM yyyy"));

                var salesInMonth = await oppsQuery
                    .Where(o => o.Status == "Won" && o.CreatedDate >= monthStart && o.CreatedDate < monthEnd)
                    .SumAsync(o => (decimal?)o.Amount) ?? 0m;

                monthTotals.Add(salesInMonth);
            }
            model.MonthlySalesLabels = monthLabels;
            model.MonthlySalesAmounts = monthTotals;

            // Recent leads & opportunities
            model.RecentLeads = await leadsQuery
                .OrderByDescending(l => l.CreatedDate)
                .Take(5)
                .ToListAsync();

            model.RecentOpportunities = await oppsQuery
                .OrderByDescending(o => o.CreatedDate)
                .Take(5)
                .ToListAsync();

            // Upcoming followups
            model.UpcomingFollowUps = await followUpsQuery
                .Where(f => f.Status == "Planned" && f.FollowUpDate >= now.Date)
                .OrderBy(f => f.FollowUpDate)
                .Take(5)
                .ToListAsync();

            return View(model);
        }
    }
}
