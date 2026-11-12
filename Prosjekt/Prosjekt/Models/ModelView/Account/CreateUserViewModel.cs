using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView.Account
{
    public sealed class CreateUserViewModel
    {
        [Required(ErrorMessage = "Fullt navn må fylles ut.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Navnet må være mellom 2 og 100 tegn.")]
        [RegularExpression(@"^ *[A-Za-zÀ-ÖØ-öø-ÿ][A-Za-zÀ-ÖØ-öø-ÿ '\-]*$", ErrorMessage = "Navnet kan bare inneholde bokstaver, mellomrom, bindestrek og apostrof.")]
        [Display(Name = "Fullt navn")]
        public string FullName { get; init; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; init; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; init; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; init; } = string.Empty;

        [Required]
        [Display(Name = "Rolle")]
        public string Role { get; init; } = string.Empty;
    }
}
