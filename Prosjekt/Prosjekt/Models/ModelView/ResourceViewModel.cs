using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView
{
    public class ResourceViewModel
    {
        /// <summary>
        /// La inn Id for å kunne bruke denne modellen til å redigere eksisterende ressurser.
        /// </summary>
        public int Id { get; set; }

        public string? UserId { get; set; }

        public enum ResourceType
        {
            [Display(Name = "Provisjon")]
            Provision,
            [Display(Name = "Klær")]
            Clothing,
            [Display(Name = "Verktøy")]
            Tool,
            [Display(Name = "Kjøretøy")]
            Vehicle,
            [Display(Name = "Materialer")]
            Materials,
            [Display(Name = "Annet")]
            Other
        }

        [Display(Name = "Navn")]
        [Required(ErrorMessage = "Navn må fylles ut.")]
        public string Name { get; init; } = string.Empty;

        [Display(Name = "Telefonnummer")]
        [Required(ErrorMessage = "Telefonnummer må fylles ut.")]
        [Phone(ErrorMessage = "Ugyldig telefonnummer.")]
        public string PhoneNumber { get; init; } = string.Empty;

        [Display(Name = "Beskrivelse")]
        [Required(ErrorMessage = "Beskrivelse må fylles ut.")]
        public string Description { get; init; } = string.Empty;

        [Display(Name = "Antall")]
        [Range(1, 10000, ErrorMessage = "Antall må være minst 1.")]
        public int Quantity { get; init; } = 1;

        /// <summary>
        /// Hvor mange av totalen som er tildelt behov. Regnes ut, lagres ikke.
        /// </summary>
        [Display(Name = "Allokert")]
        public int Allocated { get; init; } = 0;

        public int Available => Quantity - Allocated;

        [Display(Name = "Tilgjengelighet")]
        public bool Availability { get; init; } = true;

        [Display(Name = "Breddegrad")]
        public double? Latitude { get; set; }

        [Display(Name = "Lengdegrad")]
        public double? Longitude { get; set; }

        [Display(Name = "Kategori")]
        [Required(ErrorMessage = "Du må velge en kategori.")]
        public ResourceType Category { get; init; } = ResourceType.Other;

        [Display(Name = "Tilgjengelig fra")]
        [Required(ErrorMessage = "Tilgjengelig fra-tidspunkt må registreres.")]
        public DateTime? AvailableFrom { get; init; }

        [Display(Name = "Tilgjengelig til")]
        [Required(ErrorMessage = "Tilgjengelig til-tidspunkt må registreres.")]
        public DateTime? AvailableTo { get; init; }



        [Display(Name = "Skiltnummer")]
        public string? LicensePlate { get; init; }
        
        [Display(Name = "Kjøretøytype")]
        public string? VehicleType { get; set; }

        [Display(Name = "Størrelse")]
        public string? Size { get; init; }

        [Display(Name = "Verktøytype")]
        public string? ToolType { get; set; }

        [Display(Name = "Materialtype")]
        public string? MaterialType { get; set; }

        [Display(Name = "Provisjonstype")]
        public string? ProvisionType { get; set; }
    }
}