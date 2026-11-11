using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.Entities
{
        /// <summary>
        /// Kobler et behov til en ressurs med et antall.
        /// </summary>
        public class Allocation
        {
            public int Id { get; init; }

            [Required]
            public int NeedId { get; init; }

            [Required]
            public int ResourceId { get; init; }

            [Range(1, 100000)]
            public int Quantity { get; init; } = 1;

            public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        }
}
