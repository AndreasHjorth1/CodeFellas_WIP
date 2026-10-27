using System;
using System.ComponentModel.DataAnnotations;
using Prosjekt.Utils;

namespace Prosjekt.Models.Entities
{
    public class Resource
    {
       
        public int Id { get; init; }

        [Required(ErrorMessage = "Ressursen må ha et navn")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Er tilgjengelig for oppdrag")]
        public bool IsAvailable { get; set; } = true;

        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        // --- NYE FELTER FRA KRAVSPEKKEN ---

        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;

        public DateTime? AvailableFrom { get; set; }

        public DateTime? AvailableTo { get; set; }

        // ----------------------------------

        public Coordinates Coords { get; set; }

        public DateTime CreatedAt { get; init; } = NorwegianTime.Now;
    }
}