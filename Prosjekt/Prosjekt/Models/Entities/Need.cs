using System;
using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.Entities
{
    public class Need
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(4000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;

        [StringLength(50)]
        public string Priority { get; set; } = "Medium";

        public DateTime? NeededBy { get; set; }

        // MANGLET: Feltet for om behovet er oppfylt
        public bool IsFulfilled { get; set; } = false;

        public Coordinates Coords { get; set; } = new Coordinates(0, 0);

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}