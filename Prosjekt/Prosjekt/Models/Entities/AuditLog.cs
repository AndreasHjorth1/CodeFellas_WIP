using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.Entities
{
    public class AuditLog
    {
        public int Id { get; init; }

        public DateTimeOffset Timestamp { get; init; }
            = DateTimeOffset.UtcNow;

        [MaxLength(450)]
        public string? UserId { get; init; }

        [Required]
        [MaxLength(30)]
        public string Action { get; init; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string EntityType { get; init; } = string.Empty;

        public int EntityId { get; init; }

        [MaxLength(100)]
        public string? FieldName { get; init; }

        public string? OldValue { get; init; }

        public string? NewValue { get; init; }
    }
}