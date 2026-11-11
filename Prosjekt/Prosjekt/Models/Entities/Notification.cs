using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.Entities
{
    /// <summary>
    /// Varsel til en bruker, f.eks. når ressursen deres blir tildelt et behov eller levert tilbake.
    /// </summary>
    public class Notification
    {
        public int Id { get; init; }

        [Required]
        [MaxLength(450)]
        public string UserId { get; init; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Message { get; init; } = string.Empty;

        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        public bool IsRead { get; private set; }

        /// <summary>
        /// Markerer varselet som lest. Det eneste som kan endres etter at varselet er laget.
        /// </summary>
        public void MarkAsRead() => IsRead = true;
    }
}