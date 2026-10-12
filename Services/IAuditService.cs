namespace AcxiomCRM.Services
{
    public interface IAuditService
    {
        Task LogAsync(string action, string entityName, string? recordId = null, string? oldValue = null, string? newValue = null);
        Task LogSecurityEventAsync(string action, string userName, string details, string? ipAddress = null);
    }
}
