using System.Text;

namespace Prosjekt.Utils
{
    /// <summary>
    /// Hjelpemetoder for CSV-eksporten. Semikolon er skilletegn, slik norsk Excel forventer.
    /// </summary>
    public static class Csv
    {
        public const string ContentType = "text/csv";

        private static readonly char[] SpecialCharacters = { ';', '"', '\n', '\r' };

        /// <summary>
        /// Setter teksten i anførselstegn hvis den inneholder skilletegn, anførselstegn eller linjeskift.
        /// </summary>
        public static string Text(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.IndexOfAny(SpecialCharacters) < 0) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        public static string Date(DateTime? utc) =>
            NorwegianTime.FromUtc(utc)?.ToString("dd.MM.yyyy HH:mm") ?? "";

        /// BOM først, ellers viser ikke Excel æ, ø og å riktig
        public static byte[] ToBytes(StringBuilder sb) =>
            Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }
}
