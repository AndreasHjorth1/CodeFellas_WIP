using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView.Account
{
    public sealed class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; init; } = string.Empty;
    }
}
