using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView
{
    public class NeedViewModel
    {
        [Display(Name = "Navn")]
        [Required(ErrorMessage = "Navn må fylles ut.")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Telefonnummer")]
        [Required(ErrorMessage = "Telefonnummer må fylles ut.")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Beskrivelse")]
        [Required(ErrorMessage = "Beskrivelse må fylles ut.")]
        public string Description { get; set; } = string.Empty;

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
        public NeedType Type { get; set; } = NeedType.Other;

        [Display(Name = "Tidsfrist")]
        [Required(ErrorMessage = "Et tidspunkt må registreres")]
        public DateTime? Deadline { get; set; }

        [Display(Name = "Viktighet")]
        public ImportanceLevel Importance { get; set; }

        [Display(Name = "Er oppfylt")]
        public bool IsFulfilled { get; set; } = false;

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }
    }
}