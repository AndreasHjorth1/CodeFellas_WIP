using System;
using NodaTime;

namespace Prosjekt.Utils
{
    /// <summary>
    /// Tider lagres i UTC i databasen, men vises og skrives inn i norsk tid.
    /// </summary>
    public static class NorwegianTime
    {
        private static readonly DateTimeZone Oslo = DateTimeZoneProviders.Tzdb["Europe/Oslo"];

        /// <summary>
        /// Gjør norsk tid om til UTC. Brukes når en tid skal lagres.
        /// InZoneLeniently gir en gyldig tid også når klokka stilles om.
        /// </summary>
        public static DateTime ToUtc(DateTime norsk) =>
            LocalDateTime.FromDateTime(norsk).InZoneLeniently(Oslo).ToDateTimeUtc();

        /// <summary>
        /// Gjør UTC om til norsk tid. Brukes når en tid skal vises.
        /// Databasen sier ikke at tiden er UTC, så det må settes her.
        /// </summary>
        public static DateTime FromUtc(DateTime utc) =>
            Instant.FromDateTimeUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc))
                .InZone(Oslo)
                .ToDateTimeUnspecified();

        /// Noen av tidene er valgfrie, så disse to tar imot null også
        public static DateTime? ToUtc(DateTime? norsk) =>
            norsk.HasValue ? ToUtc(norsk.Value) : null;

        public static DateTime? FromUtc(DateTime? utc) =>
            utc.HasValue ? FromUtc(utc.Value) : null;
    }
}