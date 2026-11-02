using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Prosjekt.Utils;

namespace Prosjekt.Models.ModelView
{
    /// <summary>Det brukeren kan endre på profilsiden.</summary>
    public class ProfileFormModel
    {
        [Display(Name = "Brukernavn")]
        [Required(ErrorMessage = "Brukernavn må fylles ut.")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Brukernavnet må være mellom 3 og 30 tegn.")]
        [RegularExpression(@"^[A-Za-zÆØÅæøå0-9._-]+$",
            ErrorMessage = "Bruk bare bokstaver, tall, punktum, bindestrek og understrek.")]
        public string Username { get; set; } = string.Empty;

        [Display(Name = "Mobilnummer")]
        [Required(ErrorMessage = "Mobilnummer må fylles ut.")]
        [RegularExpression(@"^\s*(\+47|0047)?(\s*\d){8}\s*$",
            ErrorMessage = "Skriv et norsk nummer med 8 siffer, for eksempel 912 34 567.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "E-post")]
        [EmailAddress(ErrorMessage = "Skriv en gyldig e-postadresse, for eksempel kari@eksempel.no.")]
        [StringLength(254, ErrorMessage = "E-postadressen er for lang.")]
        public string? Email { get; set; }

        [Display(Name = "Adresse")]
        [StringLength(200, ErrorMessage = "Adressen kan ikke være lengre enn 200 tegn.")]
        public string? Address { get; set; }
    }

    /// <summary>En ressurs slik den vises i "Mine ressurser".</summary>
    public class ProfileResourceItem
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string CategoryLabel { get; init; } = string.Empty;
        public string? SubCategory { get; init; }
        public int Quantity { get; init; }
        public bool IsAvailable { get; init; }
        public DateTime? AvailableFrom { get; init; }
        public DateTime? AvailableTo { get; init; }
        public DateTime CreatedAt { get; init; }

        /// <summary>Tidsrommet ressursen var tilgjengelig i, er passert.</summary>
        public bool IsExpired => AvailableTo.HasValue && AvailableTo.Value < NorwegianTime.Now;
    }

    /// <summary>Alt profilsiden trenger.</summary>
    public class ProfilePageViewModel
    {
        public ProfileFormModel Profile { get; set; } = new();
        public bool HasProfile { get; set; }
        public bool Saved { get; set; }

        /// <summary>Kort melding etter en handling, f.eks. "Traktor er nå utilgjengelig."</summary>
        public string? Message { get; set; }

        public List<ProfileResourceItem> Resources { get; set; } = new();

        public int AvailableCount => Resources.Count(r => r.IsAvailable && !r.IsExpired);
        public int UnavailableCount => Resources.Count(r => !r.IsAvailable);
        public int ExpiredCount => Resources.Count(r => r.IsAvailable && r.IsExpired);
    }
}