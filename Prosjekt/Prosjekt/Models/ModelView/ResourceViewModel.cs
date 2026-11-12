using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView
{
    public record ResourceViewModel
    {
        /// <summary>
        /// La inn Id for å kunne bruke denne modellen til å redigere eksisterende ressurser.
        /// </summary>
        public int Id { get; init; }

        public string? UserId { get; init; }

        /// <summary>
        /// Navnet på den som eier ressursen. Fylles bare ut for admin og koordinator.
        /// </summary>
        public string? OwnerName { get; init; }

        /// <summary>
        /// Rekkefølgen må stemme med resourceTypes i wwwroot/js/map-symbols.js.
        /// </summary>
        public enum ResourceType
        {
            [Display(Name = "Annet")]
            Other = 0,
            [Display(Name = "Provisjon")]
            Provision = 1,
            [Display(Name = "Klær")]
            Clothing = 2,
            [Display(Name = "Verktøy")]
            Tool = 3,
            [Display(Name = "Kjøretøy")]
            Vehicle = 4,
            [Display(Name = "Materialer")]
            Materials = 5,
            [Display(Name = "Kompetanse")]
            Competence = 6,
        }

        [Display(Name = "Navn")]
        [Required(ErrorMessage = "Navn må fylles ut.")]
        public string Name { get; init; } = string.Empty;

        [Display(Name = "Telefonnummer")]
        [Required(ErrorMessage = "Telefonnummer må fylles ut.")]
        [RegularExpression(@"^ *\+?( *[0-9]){8,15} *$", ErrorMessage = "Telefonnummeret må være gyldig.")]
        public string PhoneNumber { get; init; } = string.Empty;

        [Display(Name = "Beskrivelse")]
        [Required(ErrorMessage = "Beskrivelse må fylles ut.")]
        public string Description { get; init; } = string.Empty;

        [Display(Name = "Antall")]
        [Range(1, 10000, ErrorMessage = "Antall må være mellom 1 og 10000.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Antall må være et helt tall.")]
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
        public double? Latitude { get; init; }

        [Display(Name = "Lengdegrad")]
        public double? Longitude { get; init; }

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
        public string? VehicleType { get; init; }

        [Display(Name = "Størrelse")]
        public string? Size { get; init; }

        [Display(Name = "Verktøytype")]
        public string? ToolType { get; init; }

        [Display(Name = "Materialtype")]
        public string? MaterialType { get; init; }

        [Display(Name = "Provisjonstype")]
        public string? ProvisionType { get; set; }
        [Display(Name = "Yrke eller erfaringsområde")]
        public string? CompetenceType { get; set; }
    }
}