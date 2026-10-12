using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? userFilter,
            string? moduleFilter,
            string? actionFilter,
            DateTime? startDate,
            DateTime? endDate,
            int page = 1)
        {
            int pageSize = 20;
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(userFilter))
            {
                query = query.Where(a => a.UserName.Contains(userFilter));
            }

            if (!string.IsNullOrWhiteSpace(moduleFilter))
            {
                query = query.Where(a => a.EntityName == moduleFilter);
            }

            if (!string.IsNullOrWhiteSpace(actionFilter))
            {
                query = query.Where(a => a.Action == actionFilter);
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.CreatedDate >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.CreatedDate <= endOfDay);
            }

            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;

            var logs = await query
                .OrderByDescending(a => a.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.UserFilter = userFilter;
            ViewBag.ModuleFilter = moduleFilter;
            ViewBag.ActionFilter = actionFilter;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.PageNumber = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            // Distinct lists for dropdowns
            ViewBag.Modules = await _context.AuditLogs.Select(a => a.EntityName).Distinct().ToListAsync();
            ViewBag.Actions = await _context.AuditLogs.Select(a => a.Action).Distinct().ToListAsync();

            return View(logs);
        }
    }
}
