using System.ComponentModel.DataAnnotations;

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

        [Display(Name = "Breddegrad")]
        public double Latitude { get; init; }

        [Display(Name = "Lengdegrad")]
        public double Longitude { get; init; }

        [Display(Name = "Avstand i meter")]
        public double DistanceMeters { get; init; }

        [Display(Name = "Allokert")]
        public int Allocated { get; init; }

        [Display(Name = "Allokert til dette behovet")]
        public int AllocatedToThisNeed { get; init; }

        public int Available => Quantity - Allocated;

    }
}
