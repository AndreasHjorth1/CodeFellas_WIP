using Prosjekt.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView
{
    public class NeedViewModel
    {
        [Display(Name = "Navn")]
        [Required(ErrorMessage = "Navn må fylles ut.")]
        public string Name { get; set; } = string.Empty;


        [Display(Name = "Beskrivelse")]
        [Required(ErrorMessage = "Beskrivelse må fylles ut.")]
        public string Description { get; set; } = string.Empty;


        [Display(Name = "Antall som trengs")]
        [Range(1, int.MaxValue, ErrorMessage = "Totalt må være minst 1.")]
        public int Total { get; set; } = 0;


        [Display(Name = "Viktighet")]
        [Required(ErrorMessage = "Viktighet må fylles ut.")]
        public Importance Importance { get; set; } = Importance.Low;


        [Display(Name = "Breddegrad")]
        [Required(ErrorMessage = "Du må legge inn breddegrad")]
        public double? Latitude { get; set; }


        [Display(Name = "Lengdegrad")]
        [Required(ErrorMessage = "Du må legge inn lengdegrad")]
        public double? Longitude { get; set; }
    }
}