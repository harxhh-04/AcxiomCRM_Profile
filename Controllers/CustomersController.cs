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
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomersController(
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

        public async Task<IActionResult> Index(string? searchTerm, string? statusFilter, int page = 1)
        {
            int pageSize = 10;
            var query = _crmService.GetScopedCustomers(User);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(c => c.CustomerName.ToLower().Contains(term)
                    || c.Email.ToLower().Contains(term)
                    || c.Phone.Contains(term)
                    || (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(c => c.Status == statusFilter);
            }

            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            var customers = await query
                .OrderByDescending(c => c.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new CustomerIndexViewModel
            {
                Customers = customers,
                SearchTerm = searchTerm,
                StatusFilter = statusFilter,
                PageNumber = page,
                TotalPages = totalPages,
                TotalCount = totalCount
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var customer = await _crmService.GetScopedCustomers(User)
                .Include(c => c.AssignedTo)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            var opps = await _context.Opportunities
                .Where(o => o.CustomerId == id)
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            var followUps = await _context.FollowUps
                .Where(f => f.CustomerId == id)
                .OrderByDescending(f => f.FollowUpDate)
                .ToListAsync();

            var activities = await _context.Activities
                .Where(a => a.CustomerId == id)
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();

            var model = new CustomerDetailsViewModel
            {
                Customer = customer,
                Opportunities = opps,
                FollowUps = followUps,
                Activities = activities
            };

            return View(model);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Users = await GetAssignableUsersAsync();
            return View(new Customer());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            // Server-side duplicate validation
            if (!await _crmService.IsCustomerEmailUniqueAsync(customer.Email))
            {
                ModelState.AddModelError("Email", "A customer with this email address already exists.");
            }

            if (!await _crmService.IsCustomerPhoneUniqueAsync(customer.Phone))
            {
                ModelState.AddModelError("Phone", "A customer with this phone number already exists.");
            }

            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                customer.CustomerCode = await _crmService.GenerateCustomerCodeAsync();
                customer.CreatedDate = DateTime.UtcNow;
                customer.CreatedBy = userId;

                if (string.IsNullOrEmpty(customer.AssignedToId))
                {
                    customer.AssignedToId = userId;
                }

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("Create", "Customer", customer.CustomerCode, null, $"Created customer {customer.CustomerName} ({customer.Email})");

                TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' created successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Users = await GetAssignableUsersAsync();
            return View(customer);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _crmService.GetScopedCustomers(User).FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null)
            {
                return NotFound();
            }

            ViewBag.Users = await GetAssignableUsersAsync();
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Customer customer)
        {
            if (id != customer.CustomerId)
            {
                return BadRequest();
            }

            // Check if record is accessible to current user
            var existingCustomer = await _crmService.GetScopedCustomers(User).AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == id);
            if (existingCustomer == null)
            {
                return NotFound();
            }

            // Server-side duplicate validation excluding current record
            if (!await _crmService.IsCustomerEmailUniqueAsync(customer.Email, id))
            {
                ModelState.AddModelError("Email", "A customer with this email address already exists.");
            }

            if (!await _crmService.IsCustomerPhoneUniqueAsync(customer.Phone, id))
            {
                ModelState.AddModelError("Phone", "A customer with this phone number already exists.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(customer);
                    await _context.SaveChangesAsync();

                    await _auditService.LogAsync("Update", "Customer", customer.CustomerCode,
                        $"Old: {existingCustomer.CustomerName}, {existingCustomer.Status}",
                        $"New: {customer.CustomerName}, {customer.Status}");

                    TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Customers.AnyAsync(e => e.CustomerId == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            ViewBag.Users = await GetAssignableUsersAsync();
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _crmService.GetScopedCustomers(User).FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null)
            {
                return NotFound();
            }

            var code = customer.CustomerCode;
            var name = customer.CustomerName;

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Delete", "Customer", code, name, "Customer record deleted");

            TempData["SuccessMessage"] = $"Customer '{name}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<SelectList> GetAssignableUsersAsync()
        {
            var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            return new SelectList(users, "Id", "FullName");
        }
    }
}
