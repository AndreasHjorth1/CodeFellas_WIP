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
        public string Phone { get; init; } = string.Empty;

        [Display(Name = "Beskrivelse")]
        [Required(ErrorMessage = "Beskrivelse må fylles ut.")]
        public string Description { get; init; } = string.Empty;

        public enum NeedType
        {
            [Display(Name = "Mat/Vann")]
            Food,

            [Display(Name = "Medisiner")]
            Medicine,

            [Display(Name = "Transport")]
            Transport,

            [Display(Name = "Strøm/aggregat")]
            PowerGenerator,

            [Display(Name = "Snørydding")]
            SnowRemoval,

            [Display(Name = "Sand/grus")]
            SandGravel,

            [Display(Name = "Maskiner")]
            Machinery,

            [Display(Name = "Evakuering")]
            Evacuation,

            [Display(Name = "Samband")]
            Communication,

            [Display(Name = "Mannskap")]
            Personnel,

            [Display(Name = "Annet")]
            Other
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