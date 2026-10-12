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
    public class FollowUpsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public FollowUpsController(
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

        public async Task<IActionResult> Index(string? statusFilter, string? typeFilter, string? dateFilter, int page = 1)
        {
            int pageSize = 10;
            var query = _crmService.GetScopedFollowUps(User);
            var now = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(f => f.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(typeFilter))
            {
                query = query.Where(f => f.FollowUpType == typeFilter);
            }

            if (!string.IsNullOrWhiteSpace(dateFilter))
            {
                switch (dateFilter.ToLower())
                {
                    case "upcoming":
                        query = query.Where(f => f.Status == "Planned" && f.FollowUpDate >= now.Date);
                        break;
                    case "overdue":
                        query = query.Where(f => f.Status == "Planned" && f.FollowUpDate < now.Date);
                        break;
                    case "today":
                        query = query.Where(f => f.FollowUpDate.Date == now.Date);
                        break;
                }
            }

            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            var followUps = await query
                .OrderBy(f => f.FollowUpDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var allScoped = _crmService.GetScopedFollowUps(User);
            var model = new FollowUpIndexViewModel
            {
                FollowUps = followUps,
                StatusFilter = statusFilter,
                TypeFilter = typeFilter,
                DateFilter = dateFilter,
                PageNumber = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                UpcomingCount = await allScoped.CountAsync(f => f.Status == "Planned" && f.FollowUpDate >= now.Date),
                OverdueCount = await allScoped.CountAsync(f => f.Status == "Planned" && f.FollowUpDate < now.Date),
                CompletedCount = await allScoped.CountAsync(f => f.Status == "Completed")
            };

            return View(model);
        }

        public async Task<IActionResult> Create(int? customerId, int? leadId, int? opportunityId)
        {
            await PopulateDropdownsAsync(customerId, leadId, opportunityId);
            return View(new FollowUp
            {
                CustomerId = customerId,
                LeadId = leadId,
                OpportunityId = opportunityId,
                FollowUpDate = DateTime.UtcNow.AddDays(1)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FollowUp followUp)
        {
            if (!_crmService.ValidateFollowUp(followUp, out var errors))
            {
                foreach (var err in errors)
                {
                    ModelState.AddModelError(string.Empty, err);
                }
            }

            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                followUp.CreatedDate = DateTime.UtcNow;

                if (string.IsNullOrEmpty(followUp.AssignedToId))
                {
                    followUp.AssignedToId = userId;
                }

                _context.FollowUps.Add(followUp);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("Create", "FollowUp", followUp.FollowUpId.ToString(), null,
                    $"Scheduled follow-up '{followUp.Subject}' ({followUp.FollowUpType}) on {followUp.FollowUpDate:yyyy-MM-dd}");

                TempData["SuccessMessage"] = "Follow-up scheduled successfully.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(followUp.CustomerId, followUp.LeadId, followUp.OpportunityId, followUp.AssignedToId);
            return View(followUp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string? completionNotes)
        {
            var followUp = await _crmService.GetScopedFollowUps(User).FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null)
            {
                return NotFound();
            }

            followUp.Status = "Completed";
            if (!string.IsNullOrWhiteSpace(completionNotes))
            {
                followUp.Remarks = $"{followUp.Remarks} | Completed: {completionNotes}";
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Update", "FollowUp", id.ToString(), "Status: Planned", "Status: Completed");

            TempData["SuccessMessage"] = $"Follow-up '{followUp.Subject}' marked as completed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var followUp = await _crmService.GetScopedFollowUps(User).FirstOrDefaultAsync(f => f.FollowUpId == id);
            if (followUp == null)
            {
                return NotFound();
            }

            _context.FollowUps.Remove(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Delete", "FollowUp", id.ToString(), followUp.Subject, "Follow-up deleted");

            TempData["SuccessMessage"] = "Follow-up deleted successfully.";
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
