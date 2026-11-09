using Microsoft.AspNetCore.Mvc;
using Prosjekt.Controllers;
using Prosjekt.Models.Entities;
using Prosjekt.Xunit.Test.Fakes;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Prosjekt.Xunit.Test.ControllerTest
{
    public class ExportControllerTest
    {
        [Fact]
        public async Task ResourcesCsv_OnlyIncludesAvailableResources()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 1, Name = "Traktor", IsAvailable = true, Category = "Vehicle", Quantity = 1, PhoneNumber = "98765432", Coords = new Coordinates(58.1467, 7.9956) });
            repo.Resources.Add(new Resource { Id = 2, Name = "Aggregat", IsAvailable = false, Category = "Tool", Coords = new Coordinates(58.1467, 7.9956) });
            var controller = new ExportController(repo, new FakeNeedRepository());

            // Act
            var result = await controller.ResourcesCsv();

            // Assert
            var file = Assert.IsType<FileContentResult>(result);
            Assert.Equal("text/csv", file.ContentType);
            Assert.StartsWith("ressurser_tilgjengelige_", file.FileDownloadName);

            var text = System.Text.Encoding.UTF8.GetString(file.FileContents);
            Assert.Contains("1;Traktor;Kjøretøy;1;Ja;98765432", text);
            Assert.DoesNotContain("Aggregat", text);
        }

        [Fact]
        public async Task ResourcesCsv_AllResources_IncludesUnavailable()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 1, Name = "Traktor", IsAvailable = true, Category = "Vehicle", Quantity = 1, Coords = new Coordinates(58.1467, 7.9956) });
            repo.Resources.Add(new Resource { Id = 2, Name = "Aggregat", IsAvailable = false, Category = "Tool", Quantity = 2, Coords = new Coordinates(58.1467, 7.9956) });
            var controller = new ExportController(repo, new FakeNeedRepository());

            // Act
            var result = await controller.ResourcesCsv(onlyAvailable: false);

            // Assert
            var file = Assert.IsType<FileContentResult>(result);
            Assert.StartsWith("ressurser_alle_", file.FileDownloadName);

            var text = System.Text.Encoding.UTF8.GetString(file.FileContents);
            Assert.Contains("1;Traktor;Kjøretøy;1;Ja;", text);
            Assert.Contains("2;Aggregat;Verktøy;2;Nei;", text);
        }

        [Fact]
        public async Task ResourcesCsv_SortsById()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 3, Name = "Båt", IsAvailable = true, Coords = new Coordinates(59.9, 10.7) });
            repo.Resources.Add(new Resource { Id = 1, Name = "Presenning", IsAvailable = true, Coords = new Coordinates(58.1467, 7.9956) });
            repo.Resources.Add(new Resource { Id = 2, Name = "Traktor", IsAvailable = true, Coords = new Coordinates(58.46, 8.77) });
            var controller = new ExportController(repo, new FakeNeedRepository());

            // Act
            var result = await controller.ResourcesCsv();

            // Assert
            var file = Assert.IsType<FileContentResult>(result);
            var text = System.Text.Encoding.UTF8.GetString(file.FileContents);
            Assert.True(text.IndexOf("Presenning") < text.IndexOf("Traktor"));
            Assert.True(text.IndexOf("Traktor") < text.IndexOf("Båt"));
        }

        [Fact]
        public async Task ResourcesCsv_TextWithSemicolon_IsQuoted()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 1, Name = "Aggregat", Description = "Diesel; 5 kW", IsAvailable = true, Coords = new Coordinates(58.1467, 7.9956) });
            var controller = new ExportController(repo, new FakeNeedRepository());

            // Act
            var result = await controller.ResourcesCsv();

            // Assert
            var file = Assert.IsType<FileContentResult>(result);
            var text = System.Text.Encoding.UTF8.GetString(file.FileContents);
            Assert.Contains("\"Diesel; 5 kW\"", text);
        }

        [Fact]
        public async Task ResourcesGeoJson_ReturnsAvailableResourcesWithNorwegianCategory()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 1, Name = "Traktor", IsAvailable = true, Category = "Vehicle", PhoneNumber = "98765432", Coords = new Coordinates(58.46, 8.77) });
            repo.Resources.Add(new Resource { Id = 2, Name = "Aggregat", IsAvailable = false, Category = "Tool", Coords = new Coordinates(58.1467, 7.9956) });
            var controller = new ExportController(repo, new FakeNeedRepository());

            // Act
            var result = await controller.ResourcesGeoJson();

            // Assert
            var file = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/geo+json", file.ContentType);
            Assert.StartsWith("ressurser_tilgjengelige_", file.FileDownloadName);
            Assert.EndsWith(".geojson", file.FileDownloadName);

            using var json = System.Text.Json.JsonDocument.Parse(file.FileContents);
            Assert.Equal("FeatureCollection", json.RootElement.GetProperty("type").GetString());
            var ressurs = Assert.Single(json.RootElement.GetProperty("features").EnumerateArray());

            // GeoJSON har lengdegrad først
            var geometri = ressurs.GetProperty("geometry");
            Assert.Equal("Point", geometri.GetProperty("type").GetString());
            Assert.Equal(8.77, geometri.GetProperty("coordinates")[0].GetDouble());
            Assert.Equal(58.46, geometri.GetProperty("coordinates")[1].GetDouble());

            var egenskaper = ressurs.GetProperty("properties");
            Assert.Equal(1, egenskaper.GetProperty("id").GetInt32());
            Assert.Equal("Traktor", egenskaper.GetProperty("navn").GetString());
            Assert.Equal("Kjøretøy", egenskaper.GetProperty("kategori").GetString());
            Assert.True(egenskaper.GetProperty("tilgjengelig").GetBoolean());
            Assert.Equal("98765432", egenskaper.GetProperty("telefon").GetString());
        }

        [Fact]
        public async Task ResourcesGeoJson_AllResources_IncludesUnavailable()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 1, Name = "Traktor", IsAvailable = true, Coords = new Coordinates(58.46, 8.77) });
            repo.Resources.Add(new Resource { Id = 2, Name = "Aggregat", IsAvailable = false, Coords = new Coordinates(58.1467, 7.9956) });
            var controller = new ExportController(repo, new FakeNeedRepository());

            // Act
            var result = await controller.ResourcesGeoJson(onlyAvailable: false);

            // Assert
            var file = Assert.IsType<FileContentResult>(result);
            Assert.StartsWith("ressurser_alle_", file.FileDownloadName);

            using var json = System.Text.Json.JsonDocument.Parse(file.FileContents);
            var ressurser = json.RootElement.GetProperty("features").EnumerateArray().ToList();
            Assert.Equal(2, ressurser.Count);
            var aggregat = ressurser.Single(r => r.GetProperty("properties").GetProperty("navn").GetString() == "Aggregat");
            Assert.False(aggregat.GetProperty("properties").GetProperty("tilgjengelig").GetBoolean());
        }
    }
}
