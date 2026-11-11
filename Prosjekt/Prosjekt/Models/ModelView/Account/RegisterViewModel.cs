using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView.Account
{
    public sealed class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; init; } = string.Empty;

        [Required(ErrorMessage = "Telefonnummer må fylles ut.")]
        [Phone(ErrorMessage = "Ugyldig telefonnummer.")]
        [Display(Name = "Telefonnummer")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; init; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; init; } = string.Empty;
    }
}
