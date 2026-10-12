using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api
{
    [Route("api/customers")]
    [ApiController]
    public class CustomersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICrmService _crmService;
        private readonly IAuditService _auditService;

        public CustomersApiController(ApplicationDbContext context, ICrmService crmService, IAuditService auditService)
        {
            _context = context;
            _crmService = crmService;
            _auditService = auditService;
        }

        // GET: /api/customers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers([FromQuery] string? search)
        {
            var query = _context.Customers.Include(c => c.AssignedTo).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(c => c.CustomerName.ToLower().Contains(term)
                    || c.Email.ToLower().Contains(term)
                    || c.Phone.Contains(term));
            }

            var customers = await query
                .OrderByDescending(c => c.CreatedDate)
                .Select(c => new CustomerDto
                {
                    CustomerId = c.CustomerId,
                    CustomerCode = c.CustomerCode,
                    CustomerName = c.CustomerName,
                    Email = c.Email,
                    Phone = c.Phone,
                    CompanyName = c.CompanyName,
                    Address = c.Address,
                    City = c.City,
                    State = c.State,
                    Status = c.Status,
                    CreatedDate = c.CreatedDate,
                    AssignedToName = c.AssignedTo != null ? c.AssignedTo.FullName : null
                })
                .ToListAsync();

            return Ok(customers);
        }

        // GET: /api/customers/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerDto>> GetCustomer(int id)
        {
            var c = await _context.Customers
                .Include(x => x.AssignedTo)
                .FirstOrDefaultAsync(x => x.CustomerId == id);

            if (c == null)
            {
                return NotFound(new { message = $"Customer with ID {id} not found." });
            }

            var dto = new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                CreatedDate = c.CreatedDate,
                AssignedToName = c.AssignedTo?.FullName
            };

            return Ok(dto);
        }

        // POST: /api/customers
        [HttpPost]
        public async Task<ActionResult<CustomerDto>> CreateCustomer([FromBody] CreateCustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!await _crmService.IsCustomerEmailUniqueAsync(dto.Email))
            {
                return Conflict(new { message = "A customer with this email already exists." });
            }

            if (!await _crmService.IsCustomerPhoneUniqueAsync(dto.Phone))
            {
                return Conflict(new { message = "A customer with this phone number already exists." });
            }

            var code = await _crmService.GenerateCustomerCodeAsync();
            var customer = new Customer
            {
                CustomerCode = code,
                CustomerName = dto.CustomerName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                Status = "Active",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "API",
                AssignedToId = dto.AssignedToId
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Create", "Customer", customer.CustomerCode, null, $"API created customer {customer.CustomerName}");

            var resultDto = new CustomerDto
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                CreatedDate = customer.CreatedDate
            };

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerId }, resultDto);
        }

        // PUT: /api/customers/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound(new { message = $"Customer with ID {id} not found." });
            }

            if (!await _crmService.IsCustomerEmailUniqueAsync(dto.Email, id))
            {
                return Conflict(new { message = "A customer with this email already exists." });
            }

            if (!await _crmService.IsCustomerPhoneUniqueAsync(dto.Phone, id))
            {
                return Conflict(new { message = "A customer with this phone number already exists." });
            }

            customer.CustomerName = dto.CustomerName;
            customer.Email = dto.Email;
            customer.Phone = dto.Phone;
            customer.CompanyName = dto.CompanyName;
            customer.Address = dto.Address;
            customer.City = dto.City;
            customer.State = dto.State;
            customer.Status = dto.Status;
            customer.AssignedToId = dto.AssignedToId;

            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Update", "Customer", customer.CustomerCode, null, $"API updated customer {customer.CustomerName}");

            return Ok(new { message = "Customer updated successfully." });
        }

        // DELETE: /api/customers/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound(new { message = $"Customer with ID {id} not found." });
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Delete", "Customer", customer.CustomerCode, customer.CustomerName, "API deleted customer");

            return Ok(new { message = $"Customer {customer.CustomerName} deleted successfully." });
        }
    }
}
