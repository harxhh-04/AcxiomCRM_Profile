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
    public class OpportunitiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OpportunitiesController(
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

        public async Task<IActionResult> Index(string? searchTerm, string? stageFilter, string? statusFilter, int page = 1)
        {
            int pageSize = 10;
            var query = _crmService.GetScopedOpportunities(User);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(o => o.OpportunityName.ToLower().Contains(term)
                    || (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(stageFilter))
            {
                query = query.Where(o => o.Stage == stageFilter);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(o => o.Status == statusFilter);
            }

            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            var opportunities = await query
                .OrderByDescending(o => o.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalPipelineAmount = await query.Where(o => o.Status == "Open").SumAsync(o => (decimal?)o.Amount) ?? 0m;
            var totalWeightedAmount = await query.Where(o => o.Status == "Open").SumAsync(o => (decimal?)((o.Amount * o.Probability) / 100m)) ?? 0m;

            var model = new OpportunityIndexViewModel
            {
                Opportunities = opportunities,
                SearchTerm = searchTerm,
                StageFilter = stageFilter,
                StatusFilter = statusFilter,
                PageNumber = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                TotalPipelineAmount = totalPipelineAmount,
                TotalWeightedAmount = totalWeightedAmount
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var opportunity = await _crmService.GetScopedOpportunities(User)
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedTo)
                .FirstOrDefaultAsync(o => o.OpportunityId == id);

            if (opportunity == null)
            {
                return NotFound();
            }

            var followUps = await _context.FollowUps
                .Where(f => f.OpportunityId == id)
                .OrderByDescending(f => f.FollowUpDate)
                .ToListAsync();

            var activities = await _context.Activities
                .Where(a => a.OpportunityId == id)
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();

            var model = new OpportunityDetailsViewModel
            {
                Opportunity = opportunity,
                FollowUps = followUps,
                Activities = activities
            };

            return View(model);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateDropdownsAsync();
            return View(new Opportunity { ExpectedCloseDate = DateTime.UtcNow.AddDays(30) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Opportunity opportunity)
        {
            // Business validation on server
            if (!_crmService.ValidateOpportunity(opportunity, out var errors))
            {
                foreach (var err in errors)
                {
                    ModelState.AddModelError(string.Empty, err);
                }
            }

            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                opportunity.CreatedDate = DateTime.UtcNow;

                if (string.IsNullOrEmpty(opportunity.AssignedToId))
                {
                    opportunity.AssignedToId = userId;
                }

                if (opportunity.Stage == "Won") opportunity.Status = "Won";
                else if (opportunity.Stage == "Lost") opportunity.Status = "Lost";
                else opportunity.Status = "Open";

                _context.Opportunities.Add(opportunity);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("Create", "Opportunity", opportunity.OpportunityId.ToString(), null,
                    $"Created opportunity {opportunity.OpportunityName} - ${opportunity.Amount} (Stage: {opportunity.Stage})");

                TempData["SuccessMessage"] = $"Opportunity '{opportunity.OpportunityName}' created successfully.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync();
            return View(opportunity);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var opportunity = await _crmService.GetScopedOpportunities(User).FirstOrDefaultAsync(o => o.OpportunityId == id);
            if (opportunity == null)
            {
                return NotFound();
            }

            await PopulateDropdownsAsync(opportunity.CustomerId, opportunity.LeadId, opportunity.AssignedToId);
            return View(opportunity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Opportunity opportunity)
        {
            if (id != opportunity.OpportunityId)
            {
                return BadRequest();
            }

            var existingOpp = await _crmService.GetScopedOpportunities(User).AsNoTracking().FirstOrDefaultAsync(o => o.OpportunityId == id);
            if (existingOpp == null)
            {
                return NotFound();
            }

            // Business validation on server
            if (!_crmService.ValidateOpportunity(opportunity, out var errors))
            {
                foreach (var err in errors)
                {
                    ModelState.AddModelError(string.Empty, err);
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (opportunity.Stage == "Won") opportunity.Status = "Won";
                    else if (opportunity.Stage == "Lost") opportunity.Status = "Lost";
                    else opportunity.Status = "Open";

                    _context.Update(opportunity);
                    await _context.SaveChangesAsync();

                    await _auditService.LogAsync("Update", "Opportunity", opportunity.OpportunityId.ToString(),
                        $"Old: {existingOpp.Stage}, ${existingOpp.Amount}, {existingOpp.Probability}%",
                        $"New: {opportunity.Stage}, ${opportunity.Amount}, {opportunity.Probability}%");

                    TempData["SuccessMessage"] = $"Opportunity '{opportunity.OpportunityName}' updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Opportunities.AnyAsync(e => e.OpportunityId == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            await PopulateDropdownsAsync(opportunity.CustomerId, opportunity.LeadId, opportunity.AssignedToId);
            return View(opportunity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var opportunity = await _crmService.GetScopedOpportunities(User).FirstOrDefaultAsync(o => o.OpportunityId == id);
            if (opportunity == null)
            {
                return NotFound();
            }

            var name = opportunity.OpportunityName;
            _context.Opportunities.Remove(opportunity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Delete", "Opportunity", id.ToString(), name, "Opportunity deleted");

            TempData["SuccessMessage"] = $"Opportunity '{name}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync(int? selectedCustomer = null, int? selectedLead = null, string? selectedUser = null)
        {
            var customers = await _crmService.GetScopedCustomers(User).OrderBy(c => c.CustomerName).ToListAsync();
            var leads = await _crmService.GetScopedLeads(User).OrderBy(l => l.LeadName).ToListAsync();
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

            ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", selectedCustomer);
            ViewBag.Leads = new SelectList(leads, "LeadId", "LeadName", selectedLead);
            ViewBag.Users = new SelectList(users, "Id", "FullName", selectedUser);
        }
    }
}
