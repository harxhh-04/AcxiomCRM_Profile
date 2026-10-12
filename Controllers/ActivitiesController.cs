using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ActivitiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ActivitiesController(
            ApplicationDbContext context,
            ICrmService crmService,
            IAuditService auditService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _crmService = crmService;
            _auditService = auditService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? typeFilter, string? statusFilter, string? searchTerm, int page = 1)
        {
            int pageSize = 10;
            var query = _crmService.GetScopedActivities(User);

            if (!string.IsNullOrWhiteSpace(typeFilter))
            {
                query = query.Where(a => a.ActivityType == typeFilter);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(a => a.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(a => a.Subject.ToLower().Contains(term)
                    || (a.Description != null && a.Description.ToLower().Contains(term)));
            }

            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            var activities = await query
                .OrderByDescending(a => a.ActivityDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new ActivityIndexViewModel
            {
                Activities = activities,
                TypeFilter = typeFilter,
                StatusFilter = statusFilter,
                SearchTerm = searchTerm,
                PageNumber = page,
                TotalPages = totalPages,
                TotalCount = totalCount
            };

            return View(model);
        }

        public async Task<IActionResult> Create(int? customerId, int? leadId, int? opportunityId)
        {
            await PopulateDropdownsAsync(customerId, leadId, opportunityId);
            return View(new Activity
            {
                CustomerId = customerId,
                LeadId = leadId,
                OpportunityId = opportunityId,
                ActivityDate = DateTime.UtcNow
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Activity activity)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(activity.AssignedToId))
                {
                    activity.AssignedToId = userId;
                }

                _context.Activities.Add(activity);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("Create", "Activity", activity.ActivityId.ToString(), null,
                    $"Logged {activity.ActivityType} activity: '{activity.Subject}'");

                TempData["SuccessMessage"] = "Activity logged successfully.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(activity.CustomerId, activity.LeadId, activity.OpportunityId, activity.AssignedToId);
            return View(activity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var activity = await _crmService.GetScopedActivities(User).FirstOrDefaultAsync(a => a.ActivityId == id);
            if (activity == null)
            {
                return NotFound();
            }

            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Delete", "Activity", id.ToString(), activity.Subject, "Activity deleted");

            TempData["SuccessMessage"] = "Activity deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync(int? customerId = null, int? leadId = null, int? opportunityId = null, string? assignedToId = null)
        {
            var customers = await _crmService.GetScopedCustomers(User).OrderBy(c => c.CustomerName).ToListAsync();
            var leads = await _crmService.GetScopedLeads(User).OrderBy(l => l.LeadName).ToListAsync();
            var opps = await _crmService.GetScopedOpportunities(User).OrderBy(o => o.OpportunityName).ToListAsync();
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

            ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", customerId);
            ViewBag.Leads = new SelectList(leads, "LeadId", "LeadName", leadId);
            ViewBag.Opportunities = new SelectList(opps, "OpportunityId", "OpportunityName", opportunityId);
            ViewBag.Users = new SelectList(users, "Id", "FullName", assignedToId);
        }
    }
}
