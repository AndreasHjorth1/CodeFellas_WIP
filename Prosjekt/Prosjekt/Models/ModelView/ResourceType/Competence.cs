using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView.ResourceType
{
    /// <summary>
    /// Kompetanse som ressurs - et yrke eller erfaringsområde noen stiller til rådighet.
    /// Selve erfaringen beskrives i det vanlige Beskrivelse-feltet.
    /// </summary>
    public class CompetenceViewModel : ResourceViewModel
    {
        public enum CompetenceType
        {
            [Display(Name = "Helse og omsorg")]
            Health,
            [Display(Name = "Bygg og anlegg")]
            Construction,
            [Display(Name = "Transport og sjåfør")]
            Transport,
            [Display(Name = "Elektro")]
            Electrical,
            [Display(Name = "Matlaging og storkjøkken")]
            Catering,
            [Display(Name = "Søk og redning")]
            SearchAndRescue,
            [Display(Name = "Samband og IT")]
            Communication,
            [Display(Name = "Annet")]
            Other
        }

        [Display(Name = "Yrke eller erfaringsområde")]
        public CompetenceType Type { get; set; } = CompetenceType.Other;

        public CompetenceViewModel()
        {
            Category = ResourceType.Competence;
        }
    }
}