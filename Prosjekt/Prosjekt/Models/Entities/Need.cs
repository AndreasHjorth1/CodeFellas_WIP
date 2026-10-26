using System;
using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.Entities
{
    public class Need
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(4000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Priority { get; set; } = "Medium";

        [Required]
        public DateTime? NeededBy { get; set; }

        [Required]
        public bool IsFulfilled { get; set; } = false;

        [Required]
        public Coordinates Coords { get; set; } = new Coordinates(0, 0);

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string UserId { get; set; } = string.Empty;
    }
}