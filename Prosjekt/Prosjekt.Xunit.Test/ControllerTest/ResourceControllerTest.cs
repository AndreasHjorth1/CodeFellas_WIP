//using Microsoft.AspNetCore.Mvc;
//using Prosjekt.Controllers;
//using Prosjekt.Models.Entities;
//using Prosjekt.Models.ModelView;
//using Prosjekt.Xunit.Test.Fakes;

//namespace Prosjekt.Xunit.Test.ControllerTest
//{
//    public class ResourceControllerTest
//    {
//        [Fact]
//        public void Index_ReturnsViewWithCreatorModel()
//        {
//            // Arrange
//            var repo = new FakeResourceRepository();
//            var controller = new ResourceController(repo);

//            // Act
//            var result = controller.Index();

//            // Assert
//            var viewResult = Assert.IsType<ViewResult>(result);
//            var model = Assert.IsType<ResourceCreatorViewModel>(viewResult.Model);
//            Assert.Empty(model.ResourceList);
//        }

//        [Fact]
//        public async Task Create_InvalidModelState_ReturnsIndexView()
//        {
//            // Arrange
//            var repo = new FakeResourceRepository();
//            var controller = new ResourceController(repo);
//            controller.ModelState.AddModelError("Latitude", "Required");

//            var model = new ResourceCreatorViewModel
//            {
//                ResourceList = new List<ResourceViewModel>
//                  {
//                      new ResourceViewModel { Name = "Aggregat" }
//                  }
//            };

//            // Act
//            var result = await controller.Create(model);

//            // Assert
//            var viewResult = Assert.IsType<ViewResult>(result);
//            Assert.Equal("Index", viewResult.ViewName);
//            Assert.Empty(repo.Resources);
//        }

//        [Fact]
//        public async Task Create_EmptyList_ReturnsIndexViewWithError()
//        {
//            // Arrange
//            var repo = new FakeResourceRepository();
//            var controller = new ResourceController(repo);

//            var model = new ResourceCreatorViewModel
//            {
//                ResourceList = new List<ResourceViewModel>(),
//                Latitude = 58.1467,
//                Longitude = 7.9956
//            };

//            // Act
//            var result = await controller.Create(model);

//            // Assert
//            var viewResult = Assert.IsType<ViewResult>(result);
//            Assert.Equal("Index", viewResult.ViewName);
//            Assert.False(controller.ModelState.IsValid);
//            Assert.Empty(repo.Resources);
//        }

//        [Fact]
//        public async Task Create_InvalidCoordinates_ReturnsIndexViewWithError()
//        {
//            // Arrange
//            var repo = new FakeResourceRepository();
//            var controller = new ResourceController(repo);

//            var model = new ResourceCreatorViewModel
//            {
//                ResourceList = new List<ResourceViewModel>
//                  {
//                      new ResourceViewModel { Name = "Aggregat" }
//                  },
//                Latitude = 200,
//                Longitude = 10
//            };

//            // Act
//            var result = await controller.Create(model);

//            // Assert
//            var viewResult = Assert.IsType<ViewResult>(result);
//            Assert.Equal("Index", viewResult.ViewName);
//            Assert.False(controller.ModelState.IsValid);
//            Assert.Empty(repo.Resources);
//        }

//        [Fact]
//        public async Task Create_ValidModel_SavesResourceAndReturnsCreateView()
//        {
//            // Arrange
//            var repo = new FakeResourceRepository();
//            var controller = new ResourceController(repo);

//            var model = new ResourceCreatorViewModel
//            {
//                ResourceList = new List<ResourceViewModel>
//                  {
//                      new ResourceViewModel { Name = "Aggregat", Quantity = 2 }
//                  },
//                Latitude = 58.1467,
//                Longitude = 7.9956
//            };

//            // Act
//            var result = await controller.Create(model);

//            // Assert
//            var viewResult = Assert.IsType<ViewResult>(result);
//            Assert.Equal("Create", viewResult.ViewName);

//            var savedResource = Assert.Single(repo.Resources);
//            Assert.Equal("Aggregat", savedResource.Name);
//            Assert.Equal(2, savedResource.Quantity);
//            Assert.Equal(58.1467, savedResource.Coords.Latitude);
//        }

//        [Fact]
//        public async Task Overview_ReturnsViewWithAllResources()
//        {
//            // Arrange
//            var repo = new FakeResourceRepository();
//            repo.Resources.Add(new Resource { Id = 1, Name = "Aggregat", Coords = new Coordinates(58.1467, 7.9956) });
//            repo.Resources.Add(new Resource { Id = 2, Name = "Båt", Coords = new Coordinates(58.1467, 7.9956) });
//            var controller = new ResourceController(repo);

//            // Act
//            var result = await controller.Overview();

//            // Assert
//            var viewResult = Assert.IsType<ViewResult>(result);
//            var model = Assert.IsType<List<ResourceViewModel>>(viewResult.Model);
//            Assert.Equal(2, model.Count);
//            Assert.Contains(model, r => r.Name == "Aggregat");
//            Assert.Contains(model, r => r.Name == "Båt");
//        }

//        [Fact]
//        public async Task Edit_Get_UnknownId_ReturnsNotFound()
//        {
//            // Arrange
//            var repo = new FakeResourceRepository();
//            var controller = new ResourceController(repo);

//            // Act
//            var result = await controller.Edit(99);

//            // Assert
//            Assert.IsType<NotFoundResult>(result);
//        }

//        [Fact]
//        public async Task Edit_Get_ExistingId_ReturnsViewWithResource()
//        {
//            // Arrange
//            var repo = new FakeResourceRepository();
//            repo.Resources.Add(new Resource { Id = 1, Name = "Aggregat", Coords = new Coordinates(58.1467, 7.9956) });
//            var controller = new ResourceController(repo);

//            // Act
//            var result = await controller.Edit(1);

//            // Assert
//            var viewResult = Assert.IsType<ViewResult>(result);
//            var model = Assert.IsType<ResourceViewModel>(viewResult.Model);
//            Assert.Equal(1, model.Id);
//            Assert.Equal("Aggregat", model.Name);
//            Assert.Equal(58.1467, model.Latitude);
//        }

//        [Fact]
//        public async Task Edit_Post_IdMismatch_ReturnsBadRequest()
//        {
//            // Arrange
//            var repo = new FakeResourceRepository();
//            var controller = new ResourceController(repo);
//            var model = new ResourceViewModel { Id = 2, Name = "Aggregat", Latitude = 58.1467, Longitude = 7.9956 };

//            // Act
//            var result = await controller.Edit(1, model);

//            // Assert
//            Assert.IsType<BadRequestResult>(result);
//        }

//        [Fact]
//        public async Task Edit_Post_MissingCoordinates_ReturnsViewWithError()
//        {
//            // Arrange
//            var repo = new FakeResourceRepository();
//            var controller = new ResourceController(repo);
//            var model = new ResourceViewModel { Id = 1, Name = "Aggregat" };

//            // Act
//            var result = await controller.Edit(1, model);

//            // Assert
//            var viewResult = Assert.IsType<ViewResult>(result);
//            Assert.Same(model, viewResult.Model);
//            Assert.False(controller.ModelState.IsValid);
//        }

//        [Fact]
//        public async Task Edit_Post_UnknownId_ReturnsNotFound()
//        {
//            // Arrange
//            var repo = new FakeResourceRepository();
//            var controller = new ResourceController(repo);
//            var model = new ResourceViewModel { Id = 99, Name = "Aggregat", Latitude = 58.1467, Longitude = 7.9956 };

//            // Act
//            var result = await controller.Edit(99, model);

//            // Assert
//            Assert.IsType<NotFoundResult>(result);
//        }

//        [Fact]
//        public async Task Edit_Post_ValidModel_UpdatesResourceAndRedirects()
//        {
//            // Arrange
//            var repo = new FakeResourceRepository();
//            repo.Resources.Add(new Resource { Id = 1, Name = "Aggregat", Coords = new Coordinates(58.1467, 7.9956) });
//            var controller = new ResourceController(repo);
//            var model = new ResourceViewModel
//            {
//                Id = 1,
//                Name = "Båt",
//                Quantity = 3,
//                Availability = false,
//                Latitude = 59.9,
//                Longitude = 10.7
//            };

//            // Act
//            var result = await controller.Edit(1, model);

//            // Assert
//            var redirect = Assert.IsType<RedirectToActionResult>(result);
//            Assert.Equal("Overview", redirect.ActionName);

//            var updatedResource = Assert.Single(repo.Resources);
//            Assert.Equal("Båt", updatedResource.Name);
//            Assert.Equal(3, updatedResource.Quantity);
//            Assert.False(updatedResource.IsAvailable);
//            Assert.Equal(59.9, updatedResource.Coords.Latitude);
//        }

//        [Fact]
//        public async Task Delete_RemovesResourceAndRedirectsToOverview()
//        {
//            // Arrange
//            var repo = new FakeResourceRepository();
//            repo.Resources.Add(new Resource { Id = 1, Name = "Aggregat", Coords = new Coordinates(58.1467, 7.9956) });
//            var controller = new ResourceController(repo);

//            // Act
//            var result = await controller.Delete(1);

//            // Assert
//            var redirect = Assert.IsType<RedirectToActionResult>(result);
//            Assert.Equal("Overview", redirect.ActionName);
//            Assert.Empty(repo.Resources);
//        }
//    }
//}