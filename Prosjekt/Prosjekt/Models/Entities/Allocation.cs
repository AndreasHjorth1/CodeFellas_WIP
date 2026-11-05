using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.Entities
{
        /// <summary>
        /// Kobler et behov til en ressurs med et antall.
        /// </summary>
        public class Allocation
        {
            public int Id { get; set; }

            [Required]
            public int NeedId { get; set; }

            [Required]
            public int ResourceId { get; set; }

            [Range(1, 100000)]
            public int Quantity { get; set; } = 1;

            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }
}
