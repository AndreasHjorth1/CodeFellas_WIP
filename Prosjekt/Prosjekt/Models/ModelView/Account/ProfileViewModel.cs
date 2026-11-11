using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView.Account
{
    /// <summary>
    /// Brukerens egen profilside. Navn og e-post vises bare, telefonnummeret kan endres.
    /// </summary>
    public class ProfileViewModel
    {
        [Display(Name = "Navn")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "E-post")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Telefonnummer")]
        [Required(ErrorMessage = "Telefonnummer må fylles ut.")]
        [Phone(ErrorMessage = "Ugyldig telefonnummer.")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}