using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView
{
    public class ResourceMatchViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Navn")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Beskrivelse")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Kategori")]
        public string Category { get; set; } = string.Empty;

        [Display(Name = "Antall")]
        public int Quantity { get; set; }

        [Display(Name = "Telefonnummer")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Breddegrad")]
        public double Latitude { get; set; }

        [Display(Name = "Lengdegrad")]
        public double Longitude { get; set; }

        [Display(Name = "Avstand i meter")]
        public double DistanceMeters { get; set; }

        [Display(Name = "Allokert")]
        public int Allocated { get; set; }

        [Display(Name = "Allokert til dette behovet")]
        public int AllocatedToThisNeed { get; set; }

        public int Available => Quantity - Allocated;

    }
}
