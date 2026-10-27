using System;
using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView
{
    public class ResourceViewModel
    {
        public int Id { get; set; }

        public enum ResourceType
        {
            [Display(Name = "Provisjon")] Provision,
            [Display(Name = "Klær")] Clothing,
            [Display(Name = "Verktøy")] Tool,
            [Display(Name = "Kjøretøy")] Vehicle,
            [Display(Name = "Materialer")] Materials,
            [Display(Name = "Annet")] Other
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

        [Display(Name = "Tilgjengelighet")]
        public bool Availability { get; init; } = true;

        [Display(Name = "Breddegrad")]
        [Required(ErrorMessage = "Du må velge en posisjon i kartet (Breddegrad).")]
        public double? Latitude { get; set; }

        [Display(Name = "Lengdegrad")]
        [Required(ErrorMessage = "Du må velge en posisjon i kartet (Lengdegrad).")]
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

        public string? LicensePlate { get; init; }
        public string? Size { get; init; }
    }
}