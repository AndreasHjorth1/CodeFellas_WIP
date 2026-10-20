using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.Enums;

///Statusen en ressurs kan ha.
public enum ResourceStatus
{
    [Display(Name = "Tilgjengelig")] Available,
    [Display(Name = "Tildelt")] Assigned,
    [Display(Name = "Utilgjengelig")] Unavailable
}
