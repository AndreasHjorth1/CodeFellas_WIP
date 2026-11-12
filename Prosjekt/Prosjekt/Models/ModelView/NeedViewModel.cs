using System.ComponentModel.DataAnnotations;
using Prosjekt.Models.Enums;

namespace Prosjekt.Models.ModelView
{
    public class NeedViewModel
    {
        /// <summary>
        /// La inn Id for å kunne bruke denne modellen til å redigere eksisterende behov.
        /// </summary>
        public int Id { get; init; }

        [Display(Name = "Navn")]
        [Required(ErrorMessage = "Navn må fylles ut.")]
        public string Name { get; init; } = string.Empty;

        [Display(Name = "Telefonnummer")]
        [Required(ErrorMessage = "Telefonnummer må fylles ut.")]
        [RegularExpression(@"^ *\+?( *[0-9]){8,15} *$", ErrorMessage = "Telefonnummeret må være gyldig.")]
        public string Phone { get; init; } = string.Empty;

        /// <summary>
        /// Navnet på den som meldte behovet. Vises bare på oversikten, som er for admin og koordinator.
        /// </summary>
        public string? ReportedBy { get; init; }

        [Display(Name = "Beskrivelse")]
        [Required(ErrorMessage = "Beskrivelse må fylles ut.")]
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// Rekkefølgen må stemme med needTypes i wwwroot/js/map-symbols.js.
        /// </summary>
        public enum NeedType
        {
            [Display(Name = "Annet")]
            Other = 0,

            [Display(Name = "Mat/Vann")]
            Food = 1,

            [Display(Name = "Medisiner")]
            Medicine = 2,

            [Display(Name = "Transport")]
            Transport = 3,

            [Display(Name = "Strøm/aggregat")]
            PowerGenerator = 4,

            [Display(Name = "Snørydding")]
            SnowRemoval = 5,

            [Display(Name = "Sand/grus")]
            SandGravel = 6,

            [Display(Name = "Maskiner")]
            Machinery = 7,

            [Display(Name = "Evakuering")]
            Evacuation = 8,

            [Display(Name = "Samband")]
            Communication = 9,

            [Display(Name = "Mannskap")]
            Personnel = 10
        }

        [Display(Name = "Behovstype")]
        public NeedType Type { get; init; } = NeedType.Other;

        [Display(Name = "Tidsfrist")]
        [Required(ErrorMessage = "EN tidsfrist må registreres")]
        public DateTime? Deadline { get; init; }

        [Display(Name = "Viktighet")]
        public ImportanceLevel Importance { get; init; }

        [Display(Name = "Status")]
        public EntryStatus Status {get; init;} = EntryStatus.New;

        [Display(Name = "Breddegrad")]
        public double? Latitude { get; init; }

        [Display(Name = "Lengdegrad")]
        public double? Longitude { get; init; }

        /// <summary>
        /// La inn område for behovet.
        /// Dette brukes til å tegne et polygon i kartet,
        /// og kan brukes til å finne ressurser som ligger innenfor området.
        /// </summary>
        public string? AreaGeoJson { get; init; }

        public double? AreaSquareMeters { get; init; }

        public double? PerimeterMeters { get; init; }
    }
}