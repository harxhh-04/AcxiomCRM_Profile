using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;

namespace AcxiomCRM.Controllers.Api
{
    [Route("api/reports")]
    [ApiController]
    public class ReportsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /api/reports/pipeline
        [HttpGet("pipeline")]
        public async Task<ActionResult<PipelineReportDto>> GetPipelineReport()
        {
            var opps = await _context.Opportunities.ToListAsync();

            var openOpps = opps.Where(o => o.Status == "Open").ToList();
            var totalPipeline = openOpps.Sum(o => o.Amount);
            var totalWeighted = openOpps.Sum(o => (o.Amount * o.Probability) / 100m);

            var stageBreakdown = opps
                .GroupBy(o => o.Stage)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var dto = new PipelineReportDto
            {
                TotalPipelineValue = totalPipeline,
                TotalWeightedValue = totalWeighted,
                TotalOpenOpportunities = openOpps.Count,
                StageBreakdown = stageBreakdown
            };

            return Ok(dto);
        }
    }
}
