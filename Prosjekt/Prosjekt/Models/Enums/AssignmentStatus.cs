using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.Enums;

///Statusen en tildeling kan ha.
public enum AssignmentStatus
{
    [Display(Name = "Aktiv")] Active,
    [Display(Name = "Fullført")] Completed,
    [Display(Name = "Avbrutt")] Cancelled
}
