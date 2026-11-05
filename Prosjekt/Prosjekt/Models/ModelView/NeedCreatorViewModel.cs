using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView
{
    public enum ImportanceLevel
    {
        [Display(Name = "Akutt behov")]
        Acute,
        [Display(Name = "Planlagt behov")]
        Planned,
    }
    public class NeedCreatorViewModel
    {
        [Required(ErrorMessage = "Du må legge til minst ett behov.")]
        public List<NeedViewModel> NeedList { get; set; } = new List<NeedViewModel>();

        [Display(Name = "Viktighet")]
        public ImportanceLevel Importance { get; init; } = ImportanceLevel.Planned;

        [Display(Name = "Tidsfrist")]
        [Required(ErrorMessage = "En tidsfrist må registreres")]
        public DateTime? Deadline { get; init; }

        [Display(Name = "Breddegrad")]
        [Required(ErrorMessage = "Du må legge inn breddegrad")]
        public double? Latitude { get; init; }

        [Display(Name = "Lengdegrad")]
        [Required(ErrorMessage = "Du må legge inn lengdegrad")]
        public double? Longitude { get; init; }

        /// <summary>
        /// Område tegnet i kartet.
        /// </summary>
        public string? AreaGeoJson { get; init; }

        public double? AreaSquareMeters { get; init; }

        public double? PerimeterMeters { get; init; }

    }
}
