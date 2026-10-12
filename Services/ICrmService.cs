using System.Security.Claims;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services
{
    public interface ICrmService
    {
        Task<string> GenerateCustomerCodeAsync();
        Task<string> GenerateLeadCodeAsync();
        Task<bool> IsCustomerEmailUniqueAsync(string email, int? excludeCustomerId = null);
        Task<bool> IsCustomerPhoneUniqueAsync(string phone, int? excludeCustomerId = null);
        bool ValidateOpportunity(Opportunity opp, out List<string> errors);
        bool ValidateFollowUp(FollowUp followUp, out List<string> errors);
        Task<(Customer customer, Opportunity? opportunity)> ConvertLeadAsync(int leadId, string currentUserId, bool createOpportunity, string? opportunityName = null, decimal? amount = null, DateTime? expectedCloseDate = null);
        IQueryable<Customer> GetScopedCustomers(ClaimsPrincipal user);
        IQueryable<Lead> GetScopedLeads(ClaimsPrincipal user);
        IQueryable<Opportunity> GetScopedOpportunities(ClaimsPrincipal user);
        IQueryable<FollowUp> GetScopedFollowUps(ClaimsPrincipal user);
        IQueryable<Activity> GetScopedActivities(ClaimsPrincipal user);
    }
}
