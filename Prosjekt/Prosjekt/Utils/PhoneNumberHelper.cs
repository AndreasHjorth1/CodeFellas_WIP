using System.Linq;

namespace Prosjekt.Utils
{
    /// <summary>
    /// Gjør norske telefonnumre sammenlignbare, slik at "+47 912 34 567",
    /// "0047 91234567" og "912 34 567" regnes som samme nummer.
    /// </summary>
    public static class PhoneNumberHelper
    {
        /// <summary>Returnerer bare sifrene, uten landskode 47/0047. Tom streng hvis input er tom.</summary>
        public static string Normalize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var digits = new string(input.Where(char.IsDigit).ToArray());

            if (digits.Length == 12 && digits.StartsWith("0047")) return digits[4..];
            if (digits.Length == 10 && digits.StartsWith("47")) return digits[2..];

            return digits;
        }

        /// <summary>Viser et 8-sifret nummer som "912 34 567". Andre lengder vises uendret.</summary>
        public static string Format(string? input)
        {
            var d = Normalize(input);
            return d.Length == 8 ? $"{d[..3]} {d.Substring(3, 2)} {d[5..]}" : (input ?? string.Empty).Trim();
        }

        /// <summary>True hvis begge numrene er like etter normalisering (og ikke tomme).</summary>
        public static bool AreEqual(string? a, string? b)
        {
            var na = Normalize(a);
            return na.Length > 0 && na == Normalize(b);
        }
    }
}