using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(
            ApplicationDbContext context,
            ICrmService crmService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _crmService = crmService;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Pipeline));
        }

        public async Task<IActionResult> Pipeline()
        {
            var opps = await _crmService.GetScopedOpportunities(User).ToListAsync();

            var stageReport = opps
                .GroupBy(o => o.Stage)
                .Select(g => new PipelineReportItem
                {
                    Stage = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(x => x.Amount),
                    WeightedAmount = g.Sum(x => (x.Amount * x.Probability) / 100m)
                })
                .OrderBy(x => x.Stage)
                .ToList();

            var ownerReport = opps
                .GroupBy(o => o.AssignedTo?.FullName ?? "Unassigned")
                .Select(g => new OwnerPipelineReportItem
                {
                    OwnerName = g.Key,
                    OpportunityCount = g.Count(),
                    TotalAmount = g.Sum(x => x.Amount),
                    WeightedAmount = g.Sum(x => (x.Amount * x.Probability) / 100m)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            ViewBag.StageReport = stageReport;
            ViewBag.OwnerReport = ownerReport;
            ViewBag.TotalPipeline = opps.Where(o => o.Status == "Open").Sum(o => o.Amount);
            ViewBag.TotalWeighted = opps.Where(o => o.Status == "Open").Sum(o => (o.Amount * o.Probability) / 100m);

            return View();
        }

        public async Task<IActionResult> Conversion()
        {
            var leads = await _crmService.GetScopedLeads(User).ToListAsync();
            var opps = await _crmService.GetScopedOpportunities(User).ToListAsync();

            var model = new ConversionReportViewModel
            {
                TotalLeads = leads.Count,
                ConvertedLeads = leads.Count(l => l.Status == "Converted"),
                LostLeads = leads.Count(l => l.Status == "Lost"),
                ActiveLeads = leads.Count(l => l.Status != "Converted" && l.Status != "Lost"),
                TotalConvertedOpportunityValue = opps.Where(o => o.LeadId != null).Sum(o => o.Amount)
            };

            return View(model);
        }

        public async Task<IActionResult> Customers()
        {
            var customers = await _crmService.GetScopedCustomers(User)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();

            return View(customers);
        }

        public async Task<IActionResult> Leads()
        {
            var leads = await _crmService.GetScopedLeads(User)
                .OrderByDescending(l => l.CreatedDate)
                .ToListAsync();

            return View(leads);
        }

        public async Task<IActionResult> FollowUps()
        {
            var followUps = await _crmService.GetScopedFollowUps(User)
                .OrderBy(f => f.FollowUpDate)
                .ToListAsync();

            return View(followUps);
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UserActivity()
        {
            var users = await _userManager.Users.ToListAsync();
            var reportList = new List<UserActivityReportItem>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var logs = await _context.AuditLogs.Where(l => l.UserId == user.Id || l.UserName == user.Email).ToListAsync();

                reportList.Add(new UserActivityReportItem
                {
                    UserName = user.FullName,
                    Role = roles.FirstOrDefault() ?? "None",
                    TotalActions = logs.Count,
                    CreatedRecords = logs.Count(l => l.Action == "Create"),
                    UpdatedRecords = logs.Count(l => l.Action == "Update"),
                    FollowUpsLogged = await _context.FollowUps.CountAsync(f => f.AssignedToId == user.Id),
                    LastActive = logs.OrderByDescending(l => l.CreatedDate).FirstOrDefault()?.CreatedDate
                });
            }

            return View(reportList);
        }

        public async Task<IActionResult> ExportPipelineCsv()
        {
            var opps = await _crmService.GetScopedOpportunities(User).ToListAsync();
            var sb = new StringBuilder();
            sb.AppendLine("OpportunityName,Customer,Stage,Probability,Amount,WeightedValue,ExpectedCloseDate,Status,AssignedTo");

            foreach (var o in opps)
            {
                sb.AppendLine($"\"{o.OpportunityName}\",\"{o.Customer?.CustomerName}\",\"{o.Stage}\",{o.Probability},{o.Amount},{o.WeightedValue:F2},{o.ExpectedCloseDate:yyyy-MM-dd},\"{o.Status}\",\"{o.AssignedTo?.FullName}\"");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"PipelineReport_{DateTime.UtcNow:yyyyMMdd}.csv");
        }
    }
}
