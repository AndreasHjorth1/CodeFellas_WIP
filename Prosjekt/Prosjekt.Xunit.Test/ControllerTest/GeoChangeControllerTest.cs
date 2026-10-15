using Microsoft.AspNetCore.Mvc;
using Prosjekt.Controllers;
using Prosjekt.Models.Entities;
using Prosjekt.Models.ModelView;
using Prosjekt.Xunit.Test.Fakes;

namespace Prosjekt.Xunit.Test.ControllerTest
{
    public class GeoChangeControllerTest
    {
        [Fact]
        public void CorrectMap_Get_ReturnsViewWithEmptyModel()
        {
            // Arrange
            var repo = new FakeGeoChangeRepository();
            var controller = new GeoChangeController(repo);

            // Act
            var result = controller.CorrectMap();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<GeoChangeViewModel>(viewResult.Model);
        }

        [Fact]
        public async Task CorrectMap_Post_InvalidModelState_ReturnsViewWithModel()
        {
            // Arrange
            var repo = new FakeGeoChangeRepository();
            var controller = new GeoChangeController(repo);
            controller.ModelState.AddModelError("Latitude", "Required");
            var model = new GeoChangeViewModel { Description = "Test" };

            // Act
            var result = await controller.CorrectMap(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.Empty(repo.GeoChanges);
        }

        [Fact]
        public async Task CorrectMap_Post_InvalidCoordinates_ReturnsViewWithError()
        {
            // Arrange
            var repo = new FakeGeoChangeRepository();
            var controller = new GeoChangeController(repo);
            var model = new GeoChangeViewModel
            {
                Latitude = 200,
                Longitude = 10,
                Description = "Test"
            };

            // Act
            var result = await controller.CorrectMap(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.False(controller.ModelState.IsValid);
            Assert.Empty(repo.GeoChanges);
        }

        [Fact]
        public async Task CorrectMap_Post_ValidModel_SavesAndRedirectsToOverview()
        {
            // Arrange
            var repo = new FakeGeoChangeRepository();
            var controller = new GeoChangeController(repo);
            var model = new GeoChangeViewModel
            {
                Latitude = 58.1467,
                Longitude = 7.9956,
                Description = "Ny vei",
                Radius = 200,
                SelectedChangeTypes = new List<string> { "Vei", "Bygning" }
            };

            // Act
            var result = await controller.CorrectMap(model);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("CorrectionOverview", redirect.ActionName);

            var saved = Assert.Single(repo.GeoChanges);
            Assert.Equal("Ny vei", saved.Description);
            Assert.Equal(200, saved.Radius);
            Assert.Equal("Vei, Bygning", saved.ChangeTypes);
        }

        [Fact]
        public async Task CorrectionOverview_ReturnsViewWithAllChanges()
        {
            // Arrange
            var repo = new FakeGeoChangeRepository();
            repo.GeoChanges.Add(new GeoChange
            {
                Id = 1,
                Coords = new Coordinates(58.1467, 7.9956),
                Description = "Ny vei",
                ChangeTypes = "Vei, Bygning"
            });
            var controller = new GeoChangeController(repo);

            // Act
            var result = await controller.CorrectionOverview();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<GeoChangeViewModel>>(viewResult.Model);
            var item = Assert.Single(model);
            Assert.Equal("Ny vei", item.Description);
            Assert.Equal(new List<string> { "Vei", "Bygning" }, item.SelectedChangeTypes);
        }
    }
}