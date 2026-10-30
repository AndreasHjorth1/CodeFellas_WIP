using System.Globalization;
using System.Text.Json.Serialization;

namespace Prosjekt.Services;

// Slår opp fylke og kommune for et punkt via Kartverkets åpne API (ingen nøkkel nødvendig)
public class KartverketStedsoppslag : IStedsoppslag
{
    private readonly HttpClient _http;

    public KartverketStedsoppslag(HttpClient http)
    {
        _http = http;
    }

    public async Task<Sted?> FinnStedAsync(double latitude, double longitude)
    {
        var lat = latitude.ToString(CultureInfo.InvariantCulture);
        var lng = longitude.ToString(CultureInfo.InvariantCulture);
        var url = $"https://api.kartverket.no/kommuneinfo/v1/punkt?nord={lat}&ost={lng}&koordsys=4258";

        try
        {
            var svar = await _http.GetFromJsonAsync<KartverketSvar>(url);
            if (svar == null || string.IsNullOrWhiteSpace(svar.Fylkesnavn)) return null;
            return new Sted(svar.Fylkesnavn, svar.Kommunenavn);
        }
        catch
        {
            // Oppslaget skal aldri stoppe registrering av en ressurs
            return null;
        }
    }

    private class KartverketSvar
    {
        [JsonPropertyName("fylkesnavn")] public string Fylkesnavn { get; set; } = "";
        [JsonPropertyName("kommunenavn")] public string Kommunenavn { get; set; } = "";
    }
}
