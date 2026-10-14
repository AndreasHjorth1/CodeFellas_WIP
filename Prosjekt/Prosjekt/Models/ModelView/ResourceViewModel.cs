using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Prosjekt.Models.ModelView
{
    public class ResourceViewModel
    {
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
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Telefonnummer")]
        [Required(ErrorMessage = "Telefonnummer må fylles ut.")]
        [Phone(ErrorMessage = "Ugyldig telefonnummer.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Beskrivelse")]
        [Required(ErrorMessage = "Beskrivelse må fylles ut.")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Antall")]
        [Range(1, 10000, ErrorMessage = "Antall må være minst 1.")]
        public int Quantity { get; set; } = 1;

        [Display(Name = "Tilgjengelighet")]
        public bool Availability { get; set; } = true;

        [Display(Name = "Breddegrad")]
        [Required(ErrorMessage = "Du må legge inn breddegrad")]
        public double? Latitude { get; set; }

        [Display(Name = "Lengdegrad")]
        [Required(ErrorMessage = "Du må legge inn lengdegrad")]
        public double? Longitude { get; set; }

        [Display(Name = "Kategori")]
        [Required(ErrorMessage = "Du må velge en kategori")]
        public ResourceType Category { get; set; } = ResourceType.Other;

        [Display(Name = "Tilgjengelig fra: ")]
        [Required(ErrorMessage = "Et tidspunkt må registreres")]
        public DateTime? AvailableFrom { get; set; }

        [Display(Name = "Tilgjengelig til: ")]
        [Required(ErrorMessage = "Et tidspunkt må registreres")]
        public DateTime? AvailableTo { get; set; }
    }
}