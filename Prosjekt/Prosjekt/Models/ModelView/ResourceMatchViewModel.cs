using System.ComponentModel.DataAnnotations;
using Prosjekt.Models.Entities;

namespace Prosjekt.Models.ModelView
{
    public class ResourceMatchViewModel
    {
        public int Id { get; init; }

        [Display(Name = "Navn")]
        public string Name { get; init; } = string.Empty;

        [Display(Name = "Beskrivelse")]
        public string Description { get; init; } = string.Empty;

        [Display(Name = "Kategori")]
        public string Category { get; init; } = string.Empty;

        [Display(Name = "Antall")]
        public int Quantity { get; init; }

        [Display(Name = "Telefonnummer")]
        public string PhoneNumber { get; init; } = string.Empty;

        /// <summary>
        /// Navnet på den som eier ressursen. Siden er bare for admin og koordinator.
        /// </summary>
        [Display(Name = "Eier")]
        public string? OwnerName { get; init; }

        [Display(Name = "Posisjon")]
        public Coordinates Coords { get; init; }

        [Display(Name = "Avstand i meter")]
        public double DistanceMeters { get; init; }

        [Display(Name = "Allokert")]
        public int Allocated { get; init; }

        [Display(Name = "Allokert til dette behovet")]
        public int AllocatedToThisNeed { get; init; }

        public int Available => Quantity - Allocated;

    }
}
