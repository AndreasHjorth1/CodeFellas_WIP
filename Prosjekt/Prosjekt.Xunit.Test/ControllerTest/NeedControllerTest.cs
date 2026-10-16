using Microsoft.AspNetCore.Mvc;
using Prosjekt.Controllers;
using Prosjekt.Models.Entities;
using Prosjekt.Models.ModelView;
using Prosjekt.Xunit.Test.Fakes;

namespace Prosjekt.Xunit.Test.ControllerTest
{
    public class NeedControllerTest
    {
        [Fact]
        public void Index_ReturnsViewWithOneEmptyNeed()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            var controller = new NeedController(repo);

            // Act
            var result = controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<NeedCreatorViewModel>(viewResult.Model);
            Assert.Single(model.NeedList);
        }

        [Fact]
        public async Task Create_MissingCoordinates_ReturnsIndexViewWithError()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            var controller = new NeedController(repo);

            var model = new NeedCreatorViewModel
            {
                NeedList = new List<NeedViewModel>
                {
                    new NeedViewModel { Name = "Vann" }
                }
            };

            // Act
            var result = await controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.False(controller.ModelState.IsValid);
            Assert.Empty(repo.Needs);
        }

        [Fact]
        public async Task Create_InvalidCoordinates_ReturnsIndexViewWithError()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            var controller = new NeedController(repo);

            var model = new NeedCreatorViewModel
            {
                NeedList = new List<NeedViewModel>
                {
                    new NeedViewModel { Name = "Vann" }
                },
                Latitude = 200,
                Longitude = 10
            };

            // Act
            var result = await controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.False(controller.ModelState.IsValid);
            Assert.Empty(repo.Needs);
        }

        [Fact]
        public async Task Create_AllNamesEmpty_ReturnsIndexViewWithError()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            var controller = new NeedController(repo);

            var model = new NeedCreatorViewModel
            {
                NeedList = new List<NeedViewModel>
                {
                    new NeedViewModel { Name = "" }
                },
                Latitude = 58.1467,
                Longitude = 7.9956
            };

            // Act
            var result = await controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.False(controller.ModelState.IsValid);
            Assert.Empty(repo.Needs);
        }

        [Fact]
        public async Task Create_ValidModel_AddsNeedAndRedirectsToOverview()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            var controller = new NeedController(repo);

            var model = new NeedCreatorViewModel
            {
                NeedList = new List<NeedViewModel>
                {
                    new NeedViewModel { Name = "Vann" }
                },
                Latitude = 58.1467,
                Longitude = 7.9956
            };

            // Act
            var result = await controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Create",viewResult.ViewName);

            var savedNeed = Assert.Single(repo.Needs);
            Assert.Equal("Vann", savedNeed.Name);
        }

        [Fact]
        public async Task Create_TwoNeeds_AddsBothToRepository()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            var controller = new NeedController(repo);

            var model = new NeedCreatorViewModel
            {
                NeedList = new List<NeedViewModel>
                {
                    new NeedViewModel { Name = "Vann" },
                    new NeedViewModel { Name = "Traktor" }
                },
                Latitude = 58.1467,
                Longitude = 7.9956
            };

            // Act
            var result = await controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Create", viewResult.ViewName);

            Assert.Equal(2, repo.Needs.Count);
            Assert.Contains(repo.Needs, n => n.Name == "Vann");
            Assert.Contains(repo.Needs, n => n.Name == "Traktor");
        }

        [Fact]
        public async Task Overview_ReturnsViewWithAllNeeds()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            repo.Needs.Add(new Need { Id = 1, Name = "Vann", Coords = new Coordinates(58.1467, 7.9956) });
            repo.Needs.Add(new Need { Id = 2, Name = "Traktor", Coords = new Coordinates(58.1467, 7.9956) });
            var controller = new NeedController(repo);

            // Act
            var result = await controller.Overview();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<NeedViewModel>>(viewResult.Model);
            Assert.Equal(2, model.Count);
            Assert.Contains(model, n => n.Name == "Vann");
            Assert.Contains(model, n => n.Name == "Traktor");
        }

        [Fact]
        public async Task Edit_Get_UnknownId_ReturnsNotFound()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            var controller = new NeedController(repo);

            // Act
            var result = await controller.Edit(99);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Get_ExistingId_ReturnsViewWithNeed()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            repo.Needs.Add(new Need { Id = 1, Name = "Vann", Coords = new Coordinates(58.1467, 7.9956) });
            var controller = new NeedController(repo);

            // Act
            var result = await controller.Edit(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<NeedViewModel>(viewResult.Model);
            Assert.Equal(1, model.Id);
            Assert.Equal("Vann", model.Name);
            Assert.Equal(58.1467, model.Latitude);
        }

        [Fact]
        public async Task Edit_Post_IdMismatch_ReturnsBadRequest()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            var controller = new NeedController(repo);
            var model = new NeedViewModel { Id = 2, Name = "Vann", Latitude = 58.1467, Longitude = 7.9956 };

            // Act
            var result = await controller.Edit(1, model);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Edit_Post_MissingCoordinates_ReturnsViewWithError()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            var controller = new NeedController(repo);
            var model = new NeedViewModel { Id = 1, Name = "Vann" };

            // Act
            var result = await controller.Edit(1, model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Edit_Post_UnknownId_ReturnsNotFound()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            var controller = new NeedController(repo);
            var model = new NeedViewModel { Id = 99, Name = "Vann", Latitude = 58.1467, Longitude = 7.9956 };

            // Act
            var result = await controller.Edit(99, model);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_ValidModel_UpdatesNeedAndRedirects()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            repo.Needs.Add(new Need { Id = 1, Name = "Vann", Coords = new Coordinates(58.1467, 7.9956) });
            var controller = new NeedController(repo);
            var model = new NeedViewModel
            {
                Id = 1,
                Name = "Mat",
                IsFulfilled = true,
                Latitude = 59.9,
                Longitude = 10.7
            };

            // Act
            var result = await controller.Edit(1, model);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Overview", redirect.ActionName);

            var updatedNeed = Assert.Single(repo.Needs);
            Assert.Equal("Mat", updatedNeed.Name);
            Assert.True(updatedNeed.IsFulfilled);
            Assert.Equal(59.9, updatedNeed.Coords.Latitude);
        }

        [Fact]
        public async Task Delete_RemovesNeedAndRedirectsToOverview()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            repo.Needs.Add(new Need { Id = 1, Name = "Vann", Coords = new Coordinates(58.1467, 7.9956) });
            var controller = new NeedController(repo);

            // Act
            var result = await controller.Delete(1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Overview", redirect.ActionName);
            Assert.Empty(repo.Needs);
        }
    }
}