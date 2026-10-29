using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.Enums;

///Statusen en tildeling kan ha.
/// 
public enum EntryStatus
{
    [Display(Name = "Ny")] New,
    [Display(Name = "Vurderes")] UnderReview,
    [Display(Name = "Tildelt")] Assigned,
    [Display(Name = "Løst")] Resolved
}

public static class EntryStatusFlow
{
    //Allowed: stay put, one step forward, or one step back (to undo a mistake)
    // If undo not allowed change: to == from || to == from + 1
    public static bool CanMove(EntryStatus from, EntryStatus to) => Math.Abs((int)to - (int)from) <= 1;

    public static EntryStatus? Next(EntryStatus s) => s == EntryStatus.Resolved ? null : s + 1;
}