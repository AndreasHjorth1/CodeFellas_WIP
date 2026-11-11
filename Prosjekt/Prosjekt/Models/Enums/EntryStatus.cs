using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.Enums;

/// <summary>
/// Stadiene en oppføring går gjennom, status flyt.
/// Lagres som tall i databasen, og CanMove regner på tallene, så verdiene må ikke endres.
/// </summary>
public enum EntryStatus
{
    [Display(Name = "Ny")] New = 0,
    [Display(Name = "Vurderes")] UnderReview = 1,
    [Display(Name = "Tildelt")] Assigned = 2,
    [Display(Name = "Løst")] Resolved = 3
}

public static class EntryStatusFlow
{
    /// <summary>
    /// Lar deg sjekke om det er lov å gå fra en status til en annen.
    /// For eksempel kan man gå fra New til UnderReview, men ikke fra New til Resolved.
    /// </summary>
    public static bool CanMove(EntryStatus from, EntryStatus to) => Math.Abs((int)to - (int)from) <= 1;

    public static EntryStatus? Next(EntryStatus s) => s == EntryStatus.Resolved ? null : s + 1;
}