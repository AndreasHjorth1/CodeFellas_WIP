using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Prosjekt.Controllers;
using Prosjekt.Models.Authorization;
using Prosjekt.Models.Entities;
using Prosjekt.Models.ModelView;
using Prosjekt.Xunit.Test.Fakes;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Prosjekt.Xunit.Test.ControllerTest
{
    public class ResourceControllerTest
    {
        private static ResourceController CreateController(FakeResourceRepository repo, string userId = "user-1", bool isAdmin = false)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, RoleNames.Admin));
            }

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            };
            httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            return new ResourceController(repo, new FakeAllocationRepository())
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
        }

        [Fact]
        public void Index_ReturnsViewWithCreatorModel()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            var controller = new ResourceController(repo, new FakeAllocationRepository());

            // Act
            var result = controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ResourceCreatorViewModel>(viewResult.Model);
            Assert.Empty(model.ResourceList);
        }

        [Fact]
        public async Task Create_InvalidModelState_ReturnsIndexView()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            var controller = new ResourceController(repo, new FakeAllocationRepository());
            controller.ModelState.AddModelError("Latitude", "Required");

            var model = new ResourceCreatorViewModel
            {
                ResourceList = new List<ResourceViewModel>
                {
                    new ResourceViewModel { Name = "Aggregat" }
                }
            };

            // Act
            var result = await controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Empty(repo.Resources);
        }

        [Fact]
        public async Task Create_EmptyList_ReturnsIndexViewWithError()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            var controller = new ResourceController(repo, new FakeAllocationRepository());

            var model = new ResourceCreatorViewModel
            {
                ResourceList = new List<ResourceViewModel>(),
                Latitude = 58.1467,
                Longitude = 7.9956
            };

            // Act
            var result = await controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.False(controller.ModelState.IsValid);
            Assert.Empty(repo.Resources);
        }

        [Fact]
        public async Task Create_InvalidCoordinates_ReturnsIndexViewWithError()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            var controller = new ResourceController(repo, new FakeAllocationRepository());

            var model = new ResourceCreatorViewModel
            {
                ResourceList = new List<ResourceViewModel>
                {
                    new ResourceViewModel { Name = "Aggregat" }
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
            Assert.Empty(repo.Resources);
        }

        [Fact]
        public async Task Create_ValidModel_SavesResourceAndReturnsCreateView()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            var controller = CreateController(repo);

            var model = new ResourceCreatorViewModel
            {
                ResourceList = new List<ResourceViewModel>
                {
                    new ResourceViewModel { Name = "Aggregat", Quantity = 2 }
                },
                Latitude = 58.1467,
                Longitude = 7.9956
            };

            // Act
            var result = await controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Create", viewResult.ViewName);

            var savedResource = Assert.Single(repo.Resources);
            Assert.Equal("Aggregat", savedResource.Name);
            Assert.Equal(2, savedResource.Quantity);
            Assert.Equal(58.1467, savedResource.Coords.Latitude);
        }

        [Fact]
        public async Task Overview_ReturnsViewWithAllResources()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 1, Name = "Aggregat", Coords = new Coordinates(58.1467, 7.9956) });
            repo.Resources.Add(new Resource { Id = 2, Name = "Båt", Coords = new Coordinates(58.1467, 7.9956) });
            var controller = CreateController(repo, isAdmin: true);

            // Act
            var result = await controller.Overview();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<ResourceViewModel>>(viewResult.Model);
            Assert.Equal(2, model.Count);
            Assert.Contains(model, r => r.Name == "Aggregat");
            Assert.Contains(model, r => r.Name == "Båt");
        }

        [Fact]
        public async Task Edit_Get_UnknownId_ReturnsNotFound()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            var controller = new ResourceController(repo, new FakeAllocationRepository());

            // Act
            var result = await controller.Edit(99);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Get_ExistingId_ReturnsViewWithResource()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 1, Name = "Aggregat", Coords = new Coordinates(58.1467, 7.9956) });
            var controller = CreateController(repo, isAdmin: true);

            // Act
            var result = await controller.Edit(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ResourceViewModel>(viewResult.Model);
            Assert.Equal(1, model.Id);
            Assert.Equal("Aggregat", model.Name);
            Assert.Equal(58.1467, model.Latitude);
        }

        [Fact]
        public async Task Edit_Post_IdMismatch_ReturnsBadRequest()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            var controller = new ResourceController(repo, new FakeAllocationRepository());
            var model = new ResourceViewModel { Id = 2, Name = "Aggregat", Latitude = 58.1467, Longitude = 7.9956 };

            // Act
            var result = await controller.Edit(1, model);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task Edit_Post_MissingCoordinates_ReturnsViewWithError()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            var controller = new ResourceController(repo, new FakeAllocationRepository());
            var model = new ResourceViewModel { Id = 1, Name = "Aggregat" };

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
            var repo = new FakeResourceRepository();
            var controller = new ResourceController(repo, new FakeAllocationRepository());
            var model = new ResourceViewModel { Id = 99, Name = "Aggregat", Latitude = 58.1467, Longitude = 7.9956 };

            // Act
            var result = await controller.Edit(99, model);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_ValidModel_UpdatesResourceAndRedirects()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 1, Name = "Aggregat", Coords = new Coordinates(58.1467, 7.9956) });
            var controller = CreateController(repo, isAdmin: true);
            var model = new ResourceViewModel
            {
                Id = 1,
                Name = "Båt",
                Quantity = 3,
                Availability = false,
                Latitude = 59.9,
                Longitude = 10.7
            };

            // Act
            var result = await controller.Edit(1, model);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Overview", redirect.ActionName);

            var updatedResource = Assert.Single(repo.Resources);
            Assert.Equal("Båt", updatedResource.Name);
            Assert.Equal(3, updatedResource.Quantity);
            Assert.False(updatedResource.IsAvailable);
            Assert.Equal(59.9, updatedResource.Coords.Latitude);
        }

        [Fact]
        public async Task Delete_RemovesResourceAndRedirectsToOverview()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 1, Name = "Aggregat", Coords = new Coordinates(58.1467, 7.9956) });
            var controller = CreateController(repo, isAdmin: true);

            // Act
            var result = await controller.Delete(1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Overview", redirect.ActionName);
            Assert.Empty(repo.Resources);
        }

        [Fact]
        public async Task Create_TwoResources_SavesBothAndShowsFirstWithCoordinates()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            var controller = CreateController(repo);

            var model = new ResourceCreatorViewModel
            {
                ResourceList = new List<ResourceViewModel>
                {
                    new ResourceViewModel { Name = "Aggregat" },
                    new ResourceViewModel { Name = "Båt" }
                },
                Latitude = 58.1467,
                Longitude = 7.9956
            };

            // Act
            var result = await controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Create", viewResult.ViewName);
            Assert.Equal(2, repo.Resources.Count);
            Assert.All(repo.Resources, r => Assert.Equal(58.1467, r.Coords.Latitude));

            var shown = Assert.IsType<List<ResourceViewModel>>(viewResult.Model);
            Assert.Equal(2, shown.Count);
            Assert.Equal("Aggregat", shown[0].Name);
            Assert.Equal("Båt", shown[1].Name);
            Assert.All(shown, r =>
            {
                Assert.Equal(58.1467, r.Latitude);
                Assert.Equal(7.9956, r.Longitude);
            });
        }

        [Fact]
        public async Task Create_VehicleWithLicensePlate_AddsRegNrToDescription()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            var controller = CreateController(repo);

            var model = new ResourceCreatorViewModel
            {
                ResourceList = new List<ResourceViewModel>
                {
                    new ResourceViewModel
                    {
                        Name = "Lastebil",
                        Description = "Med henger",
                        Category = ResourceViewModel.ResourceType.Vehicle,
                        LicensePlate = "AB12345"
                    }
                },
                Latitude = 58.1467,
                Longitude = 7.9956
            };

            // Act
            await controller.Create(model);

            // Assert
            var savedResource = Assert.Single(repo.Resources);
            Assert.Equal("Med henger [Reg.nr: AB12345]", savedResource.Description);
            Assert.Equal("Vehicle", savedResource.Category);
        }

        [Fact]
        public async Task Create_ClothingWithSize_AddsSizeToSubCategory()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            var controller = CreateController(repo);

            var model = new ResourceCreatorViewModel
            {
                ResourceList = new List<ResourceViewModel>
                {
                    new ResourceViewModel
                    {
                        Name = "Jakke",
                        Description = "Vinterjakke",
                        Category = ResourceViewModel.ResourceType.Clothing,
                        Size = "L"
                    }
                },
                Latitude = 58.1467,
                Longitude = 7.9956
            };

            // Act
            await controller.Create(model);

            // Assert
            var savedResource = Assert.Single(repo.Resources);
            Assert.Equal("Vinterjakke", savedResource.Description);
            Assert.Equal("L", savedResource.SubCategory);
        }

        [Fact]
        public async Task Edit_Get_DescriptionWithRegNr_ExtractsLicensePlate()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource
            {
                Id = 1,
                Name = "Lastebil",
                Description = "Med henger [Reg.nr: AB12345]",
                Category = "Vehicle",
                Coords = new Coordinates(58.1467, 7.9956)
            });
            var controller = CreateController(repo, isAdmin: true);

            // Act
            var result = await controller.Edit(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ResourceViewModel>(viewResult.Model);
            Assert.Equal("AB12345", model.LicensePlate);
            Assert.Equal("Med henger", model.Description);
            Assert.Equal(ResourceViewModel.ResourceType.Vehicle, model.Category);
        }

        [Fact]
        public async Task Edit_Get_ClothingWithSize_MapsToViewModel()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource
            {
                Id = 1,
                Name = "Jakke",
                Description = "Vinterjakke",
                Category = "Clothing",
                SubCategory = "L",
                Coords = new Coordinates(58.1467, 7.9956)
            });
            var controller = CreateController(repo, isAdmin: true);

            // Act
            var result = await controller.Edit(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ResourceViewModel>(viewResult.Model);
            Assert.Equal("L", model.Size);
            Assert.Equal("Vinterjakke", model.Description);
            Assert.Equal(ResourceViewModel.ResourceType.Clothing, model.Category);
        }

        [Fact]
        public async Task Edit_Post_InvalidCoordinates_ReturnsViewWithError()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource
            {
                Id = 1,
                Name = "Aggregat",
                Coords = new Coordinates(58.1467, 7.9956)
            });
            var controller = new ResourceController(repo, new FakeAllocationRepository());
            var model = new ResourceViewModel
            {
                Id = 1,
                Name = "Båt",
                Latitude = 200,
                Longitude = 10
            };

            // Act
            var result = await controller.Edit(1, model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.False(controller.ModelState.IsValid);
            Assert.Equal("Aggregat", repo.Resources.Single().Name);
        }

        [Fact]
        public async Task Edit_Post_InvalidModelState_ReturnsView()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource
            {
                Id = 1,
                Name = "Aggregat",
                Coords = new Coordinates(58.1467, 7.9956)
            });
            var controller = new ResourceController(repo, new FakeAllocationRepository());
            controller.ModelState.AddModelError("PhoneNumber", "Required");
            var model = new ResourceViewModel
            {
                Id = 1,
                Name = "Båt",
                Latitude = 58.1467,
                Longitude = 7.9956
            };

            // Act
            var result = await controller.Edit(1, model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.Equal("Aggregat", repo.Resources.Single().Name);
        }

        [Fact]
        public async Task Edit_Post_VehicleWithLicensePlate_SavesRegNrInDescription()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource
            {
                Id = 1,
                Name = "Lastebil",
                Coords = new Coordinates(58.1467, 7.9956)
            });
            var controller = CreateController(repo, isAdmin: true);
            var model = new ResourceViewModel
            {
                Id = 1,
                Name = "Lastebil",
                Description = "Med henger",
                Category = ResourceViewModel.ResourceType.Vehicle,
                LicensePlate = "AB12345",
                Latitude = 58.1467,
                Longitude = 7.9956
            };

            // Act
            await controller.Edit(1, model);

            // Assert
            var updatedResource = Assert.Single(repo.Resources);
            Assert.Equal("Med henger [Reg.nr: AB12345]", updatedResource.Description);
        }

        [Fact]
        public async Task Edit_Post_ClothingWithSize_SavesSizeInSubCategory()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource
            {
                Id = 1,
                Name = "Jakke",
                Coords = new Coordinates(58.1467, 7.9956)
            });
            var controller = CreateController(repo, isAdmin: true);
            var model = new ResourceViewModel
            {
                Id = 1,
                Name = "Jakke",
                Description = "Vinterjakke",
                Category = ResourceViewModel.ResourceType.Clothing,
                Size = "L",
                Latitude = 58.1467,
                Longitude = 7.9956
            };

            // Act
            await controller.Edit(1, model);

            // Assert
            var updatedResource = Assert.Single(repo.Resources);
            Assert.Equal("Vinterjakke", updatedResource.Description);
            Assert.Equal("L", updatedResource.SubCategory);
        }

        [Fact]
        public async Task GetInformation_ReturnsJsonWithAllResources()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource
            {
                Id = 1,
                Name = "Aggregat",
                Category = "Tool",
                Quantity = 2,
                IsAvailable = false,
                Coords = new Coordinates(58.1467, 7.9956)
            });
            repo.Resources.Add(new Resource
            {
                Id = 2,
                Name = "Båt",
                Coords = new Coordinates(59.9, 10.7)
            });
            var controller = CreateController(repo, isAdmin: true);

            // Act
            var result = await controller.GetInformation();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var resources = Assert.IsAssignableFrom<IEnumerable<ResourceViewModel>>(jsonResult.Value).ToList();
            Assert.Equal(2, resources.Count);

            var aggregat = resources.Single(r => r.Name == "Aggregat");
            Assert.Equal(ResourceViewModel.ResourceType.Tool, aggregat.Category);
            Assert.Equal(2, aggregat.Quantity);
            Assert.False(aggregat.Availability);
            Assert.Equal(58.1467, aggregat.Latitude);
        }

        [Fact]
        public async Task GetInformation_UnknownCategory_UsesOther()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource
            {
                Id = 1,
                Name = "Aggregat",
                Category = "FinnesIkke",
                Coords = new Coordinates(58.1467, 7.9956)
            });
            var controller = CreateController(repo, isAdmin: true);

            // Act
            var result = await controller.GetInformation();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var resource = Assert.Single(Assert.IsAssignableFrom<IEnumerable<ResourceViewModel>>(jsonResult.Value));
            Assert.Equal(ResourceViewModel.ResourceType.Other, resource.Category);
        }

        [Fact]
        public async Task ExportCsv_OnlyIncludesAvailableResources()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 1, Name = "Traktor", IsAvailable = true, Category = "Vehicle", Quantity = 1, PhoneNumber = "98765432", Coords = new Coordinates(58.1467, 7.9956) });
            repo.Resources.Add(new Resource { Id = 2, Name = "Aggregat", IsAvailable = false, Category = "Tool", Coords = new Coordinates(58.1467, 7.9956) });
            var controller = new ResourceController(repo, new FakeAllocationRepository());

            // Act
            var result = await controller.ExportCsv();

            // Assert
            var file = Assert.IsType<FileContentResult>(result);
            Assert.Equal("text/csv", file.ContentType);
            Assert.StartsWith("ressurser_tilgjengelige_", file.FileDownloadName);

            var text = System.Text.Encoding.UTF8.GetString(file.FileContents);
            Assert.Contains("1;Traktor;Kjøretøy;1;Ja;98765432", text);
            Assert.DoesNotContain("Aggregat", text);
        }

        [Fact]
        public async Task ExportCsv_AllResources_IncludesUnavailable()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 1, Name = "Traktor", IsAvailable = true, Category = "Vehicle", Quantity = 1, Coords = new Coordinates(58.1467, 7.9956) });
            repo.Resources.Add(new Resource { Id = 2, Name = "Aggregat", IsAvailable = false, Category = "Tool", Quantity = 2, Coords = new Coordinates(58.1467, 7.9956) });
            var controller = new ResourceController(repo, new FakeAllocationRepository());

            // Act
            var result = await controller.ExportCsv(bareTilgjengelige: false);

            // Assert
            var file = Assert.IsType<FileContentResult>(result);
            Assert.StartsWith("ressurser_alle_", file.FileDownloadName);

            var text = System.Text.Encoding.UTF8.GetString(file.FileContents);
            Assert.Contains("1;Traktor;Kjøretøy;1;Ja;", text);
            Assert.Contains("2;Aggregat;Verktøy;2;Nei;", text);
        }

        [Fact]
        public async Task ExportCsv_SortsById()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 3, Name = "Båt", IsAvailable = true, Coords = new Coordinates(59.9, 10.7) });
            repo.Resources.Add(new Resource { Id = 1, Name = "Presenning", IsAvailable = true, Coords = new Coordinates(58.1467, 7.9956) });
            repo.Resources.Add(new Resource { Id = 2, Name = "Traktor", IsAvailable = true, Coords = new Coordinates(58.46, 8.77) });
            var controller = new ResourceController(repo, new FakeAllocationRepository());

            // Act
            var result = await controller.ExportCsv();

            // Assert
            var file = Assert.IsType<FileContentResult>(result);
            var text = System.Text.Encoding.UTF8.GetString(file.FileContents);
            Assert.True(text.IndexOf("Presenning") < text.IndexOf("Traktor"));
            Assert.True(text.IndexOf("Traktor") < text.IndexOf("Båt"));
        }

        [Fact]
        public async Task ExportCsv_TextWithSemicolon_IsQuoted()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 1, Name = "Aggregat", Description = "Diesel; 5 kW", IsAvailable = true, Coords = new Coordinates(58.1467, 7.9956) });
            var controller = new ResourceController(repo, new FakeAllocationRepository());

            // Act
            var result = await controller.ExportCsv();

            // Assert
            var file = Assert.IsType<FileContentResult>(result);
            var text = System.Text.Encoding.UTF8.GetString(file.FileContents);
            Assert.Contains("\"Diesel; 5 kW\"", text);
        }

        [Fact]
        public async Task ExportJson_ReturnsAvailableResourcesWithNorwegianCategory()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 1, Name = "Traktor", IsAvailable = true, Category = "Vehicle", PhoneNumber = "98765432", Coords = new Coordinates(58.46, 8.77) });
            repo.Resources.Add(new Resource { Id = 2, Name = "Aggregat", IsAvailable = false, Category = "Tool", Coords = new Coordinates(58.1467, 7.9956) });
            var controller = new ResourceController(repo, new FakeAllocationRepository());

            // Act
            var result = await controller.ExportJson();

            // Assert
            var file = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/json", file.ContentType);
            Assert.StartsWith("ressurser_tilgjengelige_", file.FileDownloadName);

            using var json = System.Text.Json.JsonDocument.Parse(file.FileContents);
            var ressurs = Assert.Single(json.RootElement.EnumerateArray());
            Assert.Equal(1, ressurs.GetProperty("Id").GetInt32());
            Assert.Equal("Traktor", ressurs.GetProperty("Navn").GetString());
            Assert.Equal("Kjøretøy", ressurs.GetProperty("Kategori").GetString());
            Assert.True(ressurs.GetProperty("Tilgjengelig").GetBoolean());
            Assert.Equal("98765432", ressurs.GetProperty("Telefon").GetString());
        }

        [Fact]
        public async Task ExportJson_AllResources_IncludesUnavailable()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 1, Name = "Traktor", IsAvailable = true, Coords = new Coordinates(58.46, 8.77) });
            repo.Resources.Add(new Resource { Id = 2, Name = "Aggregat", IsAvailable = false, Coords = new Coordinates(58.1467, 7.9956) });
            var controller = new ResourceController(repo, new FakeAllocationRepository());

            // Act
            var result = await controller.ExportJson(bareTilgjengelige: false);

            // Assert
            var file = Assert.IsType<FileContentResult>(result);
            Assert.StartsWith("ressurser_alle_", file.FileDownloadName);

            using var json = System.Text.Json.JsonDocument.Parse(file.FileContents);
            var ressurser = json.RootElement.EnumerateArray().ToList();
            Assert.Equal(2, ressurser.Count);
            Assert.False(ressurser.Single(r => r.GetProperty("Navn").GetString() == "Aggregat").GetProperty("Tilgjengelig").GetBoolean());
        }
    }
}