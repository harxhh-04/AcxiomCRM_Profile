using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api
{
    [Route("api/leads")]
    [ApiController]
    public class LeadsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;

        public LeadsApiController(ApplicationDbContext context, ICrmService crmService, IAuditService auditService)
        {
            _context = context;
            _crmService = crmService;
            _auditService = auditService;
        }

        // GET: /api/leads
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LeadDto>>> GetLeads([FromQuery] string? status)
        {
            var query = _context.Leads.Include(l => l.AssignedTo).AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(l => l.Status == status);
            }

            var leads = await query
                .OrderByDescending(l => l.CreatedDate)
                .Select(l => new LeadDto
                {
                    LeadId = l.LeadId,
                    LeadCode = l.LeadCode,
                    LeadName = l.LeadName,
                    Email = l.Email,
                    Phone = l.Phone,
                    CompanyName = l.CompanyName,
                    Source = l.Source,
                    Status = l.Status,
                    Priority = l.Priority,
                    ExpectedValue = l.ExpectedValue,
                    CreatedDate = l.CreatedDate,
                    AssignedToName = l.AssignedTo != null ? l.AssignedTo.FullName : null
                })
                .ToListAsync();

            return Ok(leads);
        }

        // GET: /api/leads/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<LeadDto>> GetLead(int id)
        {
            var l = await _context.Leads
                .Include(x => x.AssignedTo)
                .FirstOrDefaultAsync(x => x.LeadId == id);

            if (l == null)
            {
                return NotFound(new { message = $"Lead with ID {id} not found." });
            }

            var dto = new LeadDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                Priority = l.Priority,
                ExpectedValue = l.ExpectedValue,
                CreatedDate = l.CreatedDate,
                AssignedToName = l.AssignedTo?.FullName
            };

            return Ok(dto);
        }

        // POST: /api/leads
        [HttpPost]
        public async Task<ActionResult<LeadDto>> CreateLead([FromBody] CreateLeadDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var code = await _crmService.GenerateLeadCodeAsync();
            var lead = new Lead
            {
                LeadCode = code,
                LeadName = dto.LeadName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Source = dto.Source,
                Status = dto.Status,
                Priority = dto.Priority,
                ExpectedValue = dto.ExpectedValue,
                CreatedDate = DateTime.UtcNow,
                AssignedToId = dto.AssignedToId
            };

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Create", "Lead", lead.LeadCode, null, $"API captured lead {lead.LeadName}");

            var resultDto = new LeadDto
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Status = lead.Status,
                Priority = lead.Priority,
                ExpectedValue = lead.ExpectedValue,
                CreatedDate = lead.CreatedDate
            };

            return CreatedAtAction(nameof(GetLead), new { id = lead.LeadId }, resultDto);
        }
    }
}
