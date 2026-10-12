using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Http;

namespace AcxiomCRM.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string action, string entityName, string? recordId = null, string? oldValue = null, string? newValue = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var userId = httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
                var userName = httpContext?.User?.Identity?.Name ?? "System";
                var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1";

                var log = new AuditLog
                {
                    UserId = userId,
                    UserName = userName,
                    Action = action,
                    EntityName = entityName,
                    RecordId = recordId,
                    OldValue = oldValue,
                    NewValue = newValue,
                    CreatedDate = DateTime.UtcNow,
                    IpAddress = ipAddress
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Never allow logging failures to break business workflow
            }
        }

        public async Task LogSecurityEventAsync(string action, string userName, string details, string? ipAddress = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var resolvedIp = ipAddress ?? httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1";

                var log = new AuditLog
                {
                    UserId = userName,
                    UserName = userName,
                    Action = action,
                    EntityName = "Security",
                    RecordId = null,
                    OldValue = null,
                    NewValue = details,
                    CreatedDate = DateTime.UtcNow,
                    IpAddress = resolvedIp
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Silently handle audit failure
            }
        }
    }
}
