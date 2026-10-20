using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.Enums;

///Statusen et behov kan ha.
public enum NeedStatus
{
    [Display(Name = "Ny")] New,
    [Display(Name = "Under vurdering")] UnderReview,
    [Display(Name = "Tildelt")] Assigned,
    [Display(Name = "Løst")] Resolved
}
