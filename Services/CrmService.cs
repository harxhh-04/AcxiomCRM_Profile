using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public class CrmService : ICrmService
    {
        private readonly ApplicationDbContext _context;

        public CrmService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateCustomerCodeAsync()
        {
            var count = await _context.Customers.CountAsync();
            return $"CUST-{(count + 1001)}";
        }

        public async Task<string> GenerateLeadCodeAsync()
        {
            var count = await _context.Leads.CountAsync();
            return $"LEAD-{(count + 1001)}";
        }

        public async Task<bool> IsCustomerEmailUniqueAsync(string email, int? excludeCustomerId = null)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var query = _context.Customers.AsQueryable();
            if (excludeCustomerId.HasValue)
            {
                query = query.Where(c => c.CustomerId != excludeCustomerId.Value);
            }
            return !await query.AnyAsync(c => c.Email.ToLower() == email.Trim().ToLower());
        }

        public async Task<bool> IsCustomerPhoneUniqueAsync(string phone, int? excludeCustomerId = null)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            var query = _context.Customers.AsQueryable();
            if (excludeCustomerId.HasValue)
            {
                query = query.Where(c => c.CustomerId != excludeCustomerId.Value);
            }
            return !await query.AnyAsync(c => c.Phone.Trim() == phone.Trim());
        }

        public bool ValidateOpportunity(Opportunity opp, out List<string> errors)
        {
            errors = new List<string>();

            if (opp.Amount <= 0)
            {
                errors.Add("Opportunity Amount must be greater than 0.");
            }

            if (opp.Probability < 0 || opp.Probability > 100)
            {
                errors.Add("Probability must be between 0 and 100.");
            }

            if (opp.Status != "Lost" && opp.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                errors.Add("Expected Close Date cannot be in the past.");
            }

            return errors.Count == 0;
        }

        public bool ValidateFollowUp(FollowUp followUp, out List<string> errors)
        {
            errors = new List<string>();

            if (followUp.Status == "Planned" && followUp.FollowUpDate.Date < DateTime.UtcNow.Date)
            {
                errors.Add("Follow-up date cannot be earlier than today.");
            }

            return errors.Count == 0;
        }

        public async Task<(Customer customer, Opportunity? opportunity)> ConvertLeadAsync(
            int leadId,
            string currentUserId,
            bool createOpportunity,
            string? opportunityName = null,
            decimal? amount = null,
            DateTime? expectedCloseDate = null)
        {
            var lead = await _context.Leads.FindAsync(leadId);
            if (lead == null)
            {
                throw new InvalidOperationException("Lead not found.");
            }

            if (lead.Status == "Converted")
            {
                throw new InvalidOperationException("Lead has already been converted.");
            }

            // Create Customer from Lead
            var customerCode = await GenerateCustomerCodeAsync();
            var customer = new Customer
            {
                CustomerCode = customerCode,
                CustomerName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Status = "Active",
                CreatedDate = DateTime.UtcNow,
                CreatedBy = currentUserId,
                AssignedToId = lead.AssignedToId ?? currentUserId
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            Opportunity? opp = null;
            if (createOpportunity)
            {
                opp = new Opportunity
                {
                    OpportunityName = string.IsNullOrWhiteSpace(opportunityName) ? $"{lead.LeadName} - Deal" : opportunityName,
                    CustomerId = customer.CustomerId,
                    LeadId = lead.LeadId,
                    Amount = amount.HasValue && amount.Value > 0 ? amount.Value : (lead.ExpectedValue > 0 ? lead.ExpectedValue : 1000m),
                    Stage = "Qualification",
                    Probability = 20,
                    ExpectedCloseDate = expectedCloseDate ?? DateTime.UtcNow.AddDays(30),
                    Status = "Open",
                    CreatedDate = DateTime.UtcNow,
                    AssignedToId = customer.AssignedToId
                };

                _context.Opportunities.Add(opp);
                await _context.SaveChangesAsync();
            }

            // Mark lead as converted
            lead.Status = "Converted";
            lead.ConvertedCustomerId = customer.CustomerId;
            if (opp != null)
            {
                lead.ConvertedOpportunityId = opp.OpportunityId;
            }

            await _context.SaveChangesAsync();

            return (customer, opp);
        }

        public IQueryable<Customer> GetScopedCustomers(ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var isSalesExecutive = user.IsInRole("SalesExecutive");
            var isAdminOrManager = user.IsInRole("Admin") || user.IsInRole("Manager");

            var query = _context.Customers
                .Include(c => c.AssignedTo)
                .AsQueryable();

            if (isSalesExecutive && !isAdminOrManager)
            {
                query = query.Where(c => c.AssignedToId == userId);
            }

            return query;
        }

        public IQueryable<Lead> GetScopedLeads(ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var isSalesExecutive = user.IsInRole("SalesExecutive");
            var isAdminOrManager = user.IsInRole("Admin") || user.IsInRole("Manager");

            var query = _context.Leads
                .Include(l => l.AssignedTo)
                .AsQueryable();

            if (isSalesExecutive && !isAdminOrManager)
            {
                query = query.Where(l => l.AssignedToId == userId);
            }

            return query;
        }

        public IQueryable<Opportunity> GetScopedOpportunities(ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var isSalesExecutive = user.IsInRole("SalesExecutive");
            var isAdminOrManager = user.IsInRole("Admin") || user.IsInRole("Manager");

            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedTo)
                .AsQueryable();

            if (isSalesExecutive && !isAdminOrManager)
            {
                query = query.Where(o => o.AssignedToId == userId);
            }

            return query;
        }

        public IQueryable<FollowUp> GetScopedFollowUps(ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var isSalesExecutive = user.IsInRole("SalesExecutive");
            var isAdminOrManager = user.IsInRole("Admin") || user.IsInRole("Manager");

            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.Opportunity)
                .Include(f => f.AssignedTo)
                .AsQueryable();

            if (isSalesExecutive && !isAdminOrManager)
            {
                query = query.Where(f => f.AssignedToId == userId);
            }

            return query;
        }

        public IQueryable<Activity> GetScopedActivities(ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var isSalesExecutive = user.IsInRole("SalesExecutive");
            var isAdminOrManager = user.IsInRole("Admin") || user.IsInRole("Manager");

            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.Opportunity)
                .Include(a => a.AssignedTo)
                .AsQueryable();

            if (isSalesExecutive && !isAdminOrManager)
            {
                query = query.Where(a => a.AssignedToId == userId);
            }

            return query;
        }
    }
}
