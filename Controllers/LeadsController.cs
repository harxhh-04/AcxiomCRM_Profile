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
    public class LeadsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeadsController(
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

        public async Task<IActionResult> Index(string? searchTerm, string? statusFilter, string? assignedFilter, int page = 1)
        {
            int pageSize = 10;
            var query = _crmService.GetScopedLeads(User);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(l => l.LeadName.ToLower().Contains(term)
                    || l.Email.ToLower().Contains(term)
                    || l.Phone.Contains(term)
                    || (l.CompanyName != null && l.CompanyName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(l => l.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(assignedFilter))
            {
                query = query.Where(l => l.AssignedToId == assignedFilter);
            }

            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            var leads = await query
                .OrderByDescending(l => l.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

            var model = new LeadIndexViewModel
            {
                Leads = leads,
                SearchTerm = searchTerm,
                StatusFilter = statusFilter,
                AssignedFilter = assignedFilter,
                PageNumber = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                UsersList = users.Select(u => new SelectListItem { Value = u.Id, Text = u.FullName }).ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var lead = await _crmService.GetScopedLeads(User)
                .Include(l => l.AssignedTo)
                .Include(l => l.ConvertedCustomer)
                .Include(l => l.ConvertedOpportunity)
                .FirstOrDefaultAsync(l => l.LeadId == id);

            if (lead == null)
            {
                return NotFound();
            }

            var followUps = await _context.FollowUps
                .Where(f => f.LeadId == id)
                .OrderByDescending(f => f.FollowUpDate)
                .ToListAsync();

            var activities = await _context.Activities
                .Where(a => a.LeadId == id)
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();

            var model = new LeadDetailsViewModel
            {
                Lead = lead,
                FollowUps = followUps,
                Activities = activities
            };

            return View(model);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Users = await GetAssignableUsersAsync();
            return View(new Lead());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Lead lead)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                lead.LeadCode = await _crmService.GenerateLeadCodeAsync();
                lead.CreatedDate = DateTime.UtcNow;

                if (string.IsNullOrEmpty(lead.AssignedToId))
                {
                    lead.AssignedToId = userId;
                }

                _context.Leads.Add(lead);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("Create", "Lead", lead.LeadCode, null, $"Captured lead {lead.LeadName} ({lead.CompanyName}) - Source: {lead.Source}");

                TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' created successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Users = await GetAssignableUsersAsync();
            return View(lead);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var lead = await _crmService.GetScopedLeads(User).FirstOrDefaultAsync(l => l.LeadId == id);
            if (lead == null)
            {
                return NotFound();
            }

            ViewBag.Users = await GetAssignableUsersAsync();
            return View(lead);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Lead lead)
        {
            if (id != lead.LeadId)
            {
                return BadRequest();
            }

            var existingLead = await _crmService.GetScopedLeads(User).AsNoTracking().FirstOrDefaultAsync(l => l.LeadId == id);
            if (existingLead == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(lead);
                    await _context.SaveChangesAsync();

                    await _auditService.LogAsync("Update", "Lead", lead.LeadCode,
                        $"Old: {existingLead.Status}, {existingLead.ExpectedValue}",
                        $"New: {lead.Status}, {lead.ExpectedValue}");

                    TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Leads.AnyAsync(e => e.LeadId == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            ViewBag.Users = await GetAssignableUsersAsync();
            return View(lead);
        }

        [HttpGet]
        public async Task<IActionResult> Convert(int id)
        {
            var lead = await _crmService.GetScopedLeads(User).FirstOrDefaultAsync(l => l.LeadId == id);
            if (lead == null)
            {
                return NotFound();
            }

            if (lead.Status == "Converted")
            {
                TempData["ErrorMessage"] = "This lead has already been converted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var model = new ConvertLeadViewModel
            {
                LeadId = lead.LeadId,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                CreateOpportunity = true,
                OpportunityName = $"{lead.LeadName} - Deal",
                Amount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 5000m,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(30)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convert(ConvertLeadViewModel model)
        {
            var lead = await _crmService.GetScopedLeads(User).FirstOrDefaultAsync(l => l.LeadId == model.LeadId);
            if (lead == null)
            {
                return NotFound();
            }

            if (model.CreateOpportunity && (!model.Amount.HasValue || model.Amount.Value <= 0))
            {
                ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
            }

            if (model.CreateOpportunity && model.ExpectedCloseDate.HasValue && model.ExpectedCloseDate.Value.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
            }

            // Check if customer email already exists
            if (!await _crmService.IsCustomerEmailUniqueAsync(lead.Email))
            {
                ModelState.AddModelError(string.Empty, "A Customer with this email address already exists. Duplicate customer creation prevented.");
            }

            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

                var (customer, opportunity) = await _crmService.ConvertLeadAsync(
                    lead.LeadId,
                    userId,
                    model.CreateOpportunity,
                    model.OpportunityName,
                    model.Amount,
                    model.ExpectedCloseDate);

                await _auditService.LogAsync("Convert", "Lead", lead.LeadCode,
                    $"Lead: {lead.LeadName}",
                    $"Converted to Customer: {customer.CustomerCode}{(opportunity != null ? $", Opportunity: {opportunity.OpportunityName}" : "")}");

                TempData["SuccessMessage"] = $"Lead converted successfully to Customer '{customer.CustomerName}'!";
                return RedirectToAction("Details", "Customers", new { id = customer.CustomerId });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var lead = await _crmService.GetScopedLeads(User).FirstOrDefaultAsync(l => l.LeadId == id);
            if (lead == null)
            {
                return NotFound();
            }

            var code = lead.LeadCode;
            var name = lead.LeadName;

            _context.Leads.Remove(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Delete", "Lead", code, name, "Lead deleted");

            TempData["SuccessMessage"] = $"Lead '{name}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<SelectList> GetAssignableUsersAsync()
        {
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            return new SelectList(users, "Id", "FullName");
        }
    }
}
