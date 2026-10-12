using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api
{
    [Route("api/opportunities")]
    [ApiController]
    public class OpportunitiesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;

        public OpportunitiesApiController(ApplicationDbContext context, ICrmService crmService, IAuditService auditService)
        {
            _context = context;
            _crmService = crmService;
            _auditService = auditService;
        }

        // GET: /api/opportunities
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OpportunityDto>>> GetOpportunities([FromQuery] string? stage)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.AssignedTo)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(stage))
            {
                query = query.Where(o => o.Stage == stage);
            }

            var opps = await query
                .OrderByDescending(o => o.CreatedDate)
                .Select(o => new OpportunityDto
                {
                    OpportunityId = o.OpportunityId,
                    OpportunityName = o.OpportunityName,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer != null ? o.Customer.CustomerName : null,
                    LeadId = o.LeadId,
                    Amount = o.Amount,
                    Stage = o.Stage,
                    Probability = o.Probability,
                    WeightedValue = (o.Amount * o.Probability) / 100m,
                    ExpectedCloseDate = o.ExpectedCloseDate,
                    Status = o.Status,
                    CreatedDate = o.CreatedDate,
                    AssignedToName = o.AssignedTo != null ? o.AssignedTo.FullName : null,
                    Notes = o.Notes
                })
                .ToListAsync();

            return Ok(opps);
        }

        // GET: /api/opportunities/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<OpportunityDto>> GetOpportunity(int id)
        {
            var o = await _context.Opportunities
                .Include(x => x.Customer)
                .Include(x => x.AssignedTo)
                .FirstOrDefaultAsync(x => x.OpportunityId == id);

            if (o == null)
            {
                return NotFound(new { message = $"Opportunity with ID {id} not found." });
            }

            var dto = new OpportunityDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer?.CustomerName,
                LeadId = o.LeadId,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                WeightedValue = (o.Amount * o.Probability) / 100m,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                CreatedDate = o.CreatedDate,
                AssignedToName = o.AssignedTo?.FullName,
                Notes = o.Notes
            };

            return Ok(dto);
        }

        // POST: /api/opportunities
        [HttpPost]
        public async Task<ActionResult<OpportunityDto>> CreateOpportunity([FromBody] CreateOpportunityDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var opp = new Opportunity
            {
                OpportunityName = dto.OpportunityName,
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                Amount = dto.Amount,
                Stage = dto.Stage,
                Probability = dto.Probability,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                Status = dto.Stage == "Won" ? "Won" : (dto.Stage == "Lost" ? "Lost" : "Open"),
                CreatedDate = DateTime.UtcNow,
                AssignedToId = dto.AssignedToId,
                Notes = dto.Notes
            };

            if (!_crmService.ValidateOpportunity(opp, out var errors))
            {
                return BadRequest(new { errors });
            }

            _context.Opportunities.Add(opp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Create", "Opportunity", opp.OpportunityId.ToString(), null, $"API created opportunity {opp.OpportunityName}");

            var resultDto = new OpportunityDto
            {
                OpportunityId = opp.OpportunityId,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                WeightedValue = opp.WeightedValue,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Status = opp.Status,
                CreatedDate = opp.CreatedDate
            };

            return CreatedAtAction(nameof(GetOpportunity), new { id = opp.OpportunityId }, resultDto);
        }
    }
}
