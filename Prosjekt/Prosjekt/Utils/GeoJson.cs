using System.Text.Encodings.Web;
using System.Text.Json;
using NetTopologySuite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;

namespace Prosjekt.Utils
{
    /// <summary>
    /// Felles GeoJSON-kode for kartdataene og eksporten. Koordinatene er i WGS 84 (SRID 4326).
    /// </summary>
    public static class GeoJson
    {
        public const string ContentType = "application/geo+json";

        private static readonly GeometryFactory Factory =
            NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new GeoJsonConverterFactory() }
        };

        /// GeoJSON bruker rekkefølgen lengdegrad, breddegrad
        public static Point Point(double latitude, double longitude) =>
            Factory.CreatePoint(new Coordinate(longitude, latitude));

        /// <summary>
        /// Leser et tegnet område. Leaflet lagrer en hel Feature, men en ren geometri godtas også.
        /// Gir null hvis området mangler eller ikke er gyldig GeoJSON.
        /// </summary>
        public static Geometry? ParseArea(string? areaGeoJson)
        {
            if (string.IsNullOrWhiteSpace(areaGeoJson)) return null;

            try
            {
                using var doc = JsonDocument.Parse(areaGeoJson);
                var isFeature = doc.RootElement.TryGetProperty("type", out var type)
                    && type.GetString() == "Feature";

                // Feature må leses som IFeature, ellers feiler biblioteket
                return isFeature
                    ? doc.RootElement.Deserialize<IFeature>(Options)?.Geometry
                    : doc.RootElement.Deserialize<Geometry>(Options);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Det tegnede området hvis det finnes og er gyldig, ellers punktet.
        /// </summary>
        public static Geometry AreaOrPoint(string? areaGeoJson, double latitude, double longitude) =>
            ParseArea(areaGeoJson) ?? Point(latitude, longitude);

        public static byte[] ToBytes(FeatureCollection collection) =>
            JsonSerializer.SerializeToUtf8Bytes(collection, Options);
    }
}
