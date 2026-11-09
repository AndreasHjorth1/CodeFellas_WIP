using System.Security.Claims;
using Prosjekt.DataAccess;
using Prosjekt.Models.Entities;

namespace Prosjekt.Services
{
    public class AuditLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public void Add(
            string action,
            string entityType,
            int entityId,
            string? fieldName = null,
            string? oldValue = null,
            string? newValue = null)
        {
            var userId = _httpContextAccessor.HttpContext?
                .User.FindFirstValue(ClaimTypes.NameIdentifier);

            var log = new AuditLog
            {
                Timestamp = DateTimeOffset.UtcNow,
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                FieldName = fieldName,
                OldValue = oldValue,
                NewValue = newValue
            };

            _context.AuditLogs.Add(log);
        }
    }
}