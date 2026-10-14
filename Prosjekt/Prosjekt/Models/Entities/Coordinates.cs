using System.ComponentModel.DataAnnotations.Schema;

namespace Prosjekt.Models.Entities;

// 1. CRS er fjernet fra parentesen her
public readonly record struct Coordinates(double Latitude, double Longitude)
{
    // 2. CRS ligger nå som en egen egenskap på innsiden
    [NotMapped]
    public string CRS { get; } = "EPSG:4326";

    /// <summary>
    /// Indicates whether Latitude and Longitude are within valid geographic ranges.
    /// </summary>
    /// <returns>true if Latitude is between -90.0 and 90.0 and Longitude is between -180.0 and 180.0; otherwise, false.</returns>
    public bool IsValid() =>
        Latitude is >= -90.0 and <= 90.0 &&
        Longitude is >= -180.0 and <= 180.0;

    public override string ToString() => $"Lat: {Latitude}, Lng: {Longitude}, CRS: {CRS}";
}