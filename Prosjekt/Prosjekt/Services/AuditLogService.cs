using System.Security.Claims;
using Prosjekt.DataAccess;
using Prosjekt.Models.Entities;

namespace Prosjekt.Services
{
    /// <summary>
    /// AuditLogService er ansvarlig for å logge endringer i systemet til en AuditLog-tabell i databasen.
    /// Den registrerer handlinger som opprettelse, oppdatering og sletting av entiteter, 
    /// samt hvilke felter som ble endret og av hvilken bruker.
    /// </summary>
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