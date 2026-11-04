using System;
using TimeZoneConverter;

namespace Prosjekt.Utils 
{
    public static class NorwegianTime
    {
        public static DateTime Now
        {
            get
            {
                /// <summary>
                /// Henter nåværende tid i Norge.
                /// </summary>
                /// <returns>Nåværende tid i Norge.</returns>
                /// <remarks>TZConvert oversetter IANA-formatet til riktig Windows/Linux-tidssone sømløst</remarks>
                var norwayTimeZone = TZConvert.GetTimeZoneInfo("Europe/Oslo");
                               
                /// Konverterer fra UTC til nøyaktig Norsk tid
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, norwayTimeZone);
            }
        }
    }
}