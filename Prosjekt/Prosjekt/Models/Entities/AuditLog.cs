using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }

        public DateTimeOffset Timestamp { get; set; }
            = DateTimeOffset.UtcNow;

        [MaxLength(450)]
        public string? UserId { get; set; }

        [Required]
        [MaxLength(30)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;

        public int EntityId { get; set; }

        [MaxLength(100)]
        public string? FieldName { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }
    }
}