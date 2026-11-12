using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Prosjekt.Controllers;
using Prosjekt.Models.Authorization;
using Prosjekt.Models.Entities;
using Prosjekt.Models.Enums;
using Prosjekt.Models.ModelView;
using Prosjekt.Xunit.Test.Fakes;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using NetTopologySuite.Features;
using NtsPoint = NetTopologySuite.Geometries.Point;

namespace Prosjekt.Xunit.Test.ControllerTest
{
    public class ResourceControllerTest
    {
        private static FeatureCollection GetFeatures(IActionResult result)
        {
            var jsonResult = Assert.IsType<JsonResult>(result);
            return Assert.IsType<FeatureCollection>(jsonResult.Value);
        }

        private static ResourceController CreateController(FakeResourceRepository repo, string userId =
        "user-1", bool isAdmin = false, bool isCoordinator = false, FakeUserRepository? users = null,
        FakeAllocationRepository? allocations = null, FakeNeedRepository? needs = null,
        FakeNotificationRepository? notifications = null)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, RoleNames.Admin));
            }
            if (isCoordinator)
            {
                claims.Add(new Claim(ClaimTypes.Role, RoleNames.Coordinator));
            }

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            };
            httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            return new ResourceController(repo, allocations ?? new FakeAllocationRepository(), users ?? new FakeUserRepository(),
                needs ?? new FakeNeedRepository(), notifications ?? new FakeNotificationRepository())
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
        }

        /// Admin og koordinator ser hvem som eier ressursen
        [Fact]
        public async Task Overview_Coordinator_SeesOwnerName()
        {
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 1, Name = "Aggregat", UserId = "user-2", Coords = new Coordinates(58.1467, 7.9956) });
            var users = new FakeUserRepository();
            users.Names["user-2"] = "Kari Nordmann";
            var controller = CreateController(repo, isCoordinator: true, users: users);

            var result = await controller.Overview();

            var resources = Assert.IsAssignableFrom<IEnumerable<ResourceViewModel>>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal("Kari Nordmann", Assert.Single(resources).OwnerName);
        }

        /// Sikkerhet: vanlige brukere skal ikke få navn på andre brukere
        [Fact]
        public async Task Overview_RegularUser_DoesNotGetOwnerName()
        {
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 1, Name = "Aggregat", UserId = "user-1", Coords = new Coordinates(58.1467, 7.9956) });
            var users = new FakeUserRepository();
            users.Names["user-1"] = "Ola Nordmann";
            var controller = CreateController(repo, userId: "user-1", users: users);

            var result = await controller.Overview();

            var resources = Assert.IsAssignableFrom<IEnumerable<ResourceViewModel>>(Assert.IsType<ViewResult>(result).Model);
            Assert.Null(Assert.Single(resources).OwnerName);
        }

        [Fact]
        public async Task Delete_Coordinator_OtherUsersResource_ReturnsForbidAndKeepsResource()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource
            {
                Id = 1,
                Name = "Aggregat",
                UserId = "user-2",
                Coords = new
        Coordinates(58.1467, 7.9956)
            });
            var controller = CreateController(repo, isCoordinator: true);

            // Act
            var result = await controller.Delete(1);

            // Assert
            Assert.IsType<ForbidResult>(result);
            Assert.Single(repo.Resources);
        }

        [Fact]
        public async Task Delete_Coordinator_OwnResource_RemovesResource()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource
            {
                Id = 1,
                Name = "Aggregat",
                UserId = "user-1",
                Coords = new
        Coordinates(58.1467, 7.9956)
            });
            var controller = CreateController(repo, isCoordinator: true);

            // Act
            var result = await controller.Delete(1);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Empty(repo.Resources);
        }

        [Fact]
        public async Task Delete_RegularUser_OwnResource_RemovesResource()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource
            {
                Id = 1,
                Name = "Aggregat",
                UserId = "user-1",
                Coords = new
        Coordinates(58.1467, 7.9956)
            });
            var controller = CreateController(repo);

            // Act
            var result = await controller.Delete(1);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Empty(repo.Resources);
        }

        [Fact]
        public async Task Overview_Coordinator_SeesAllResources()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource
            {
                Id = 1,
                Name = "Aggregat",
                UserId = "user-2",
                Coords = new
        Coordinates(58.1467, 7.9956)
            });
            repo.Resources.Add(new Resource
            {
                Id = 2,
                Name = "Traktor",
                UserId = "user-3",
                Coords = new
        Coordinates(59.9, 10.7)
            });
            var controller = CreateController(repo, isCoordinator: true);

            // Act
            var result = await controller.Overview();

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var resources = Assert.IsAssignableFrom<IEnumerable<ResourceViewModel>>(view.Model);
            Assert.Equal(2, resources.Count());
        }

        [Fact]
        public void Index_ReturnsViewWithCreatorModel()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            var controller = new ResourceController(repo, new FakeAllocationRepository(), new FakeUserRepository(), new FakeNeedRepository(), new FakeNotificationRepository());

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
            var controller = new ResourceController(repo, new FakeAllocationRepository(), new FakeUserRepository(), new FakeNeedRepository(), new FakeNotificationRepository());
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
            var controller = new ResourceController(repo, new FakeAllocationRepository(), new FakeUserRepository(), new FakeNeedRepository(), new FakeNotificationRepository());

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
            var controller = new ResourceController(repo, new FakeAllocationRepository(), new FakeUserRepository(), new FakeNeedRepository(), new FakeNotificationRepository());

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
                    new ResourceViewModel { Name = "Aggregat", Quantity = 2, AvailableFrom = new DateTime(2026, 12, 1, 8, 0, 0), AvailableTo = new DateTime(2026, 12, 3, 16, 0, 0) }
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
        public async Task Create_RowMissingDates_ReturnsViewWithErrorAndSavesNothing()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            var controller = CreateController(repo);

            var model = new ResourceCreatorViewModel
            {
                ResourceList = new List<ResourceViewModel>
                {
                    new ResourceViewModel { Name = "Aggregat", AvailableFrom = new DateTime(2026, 12, 1, 8, 0, 0), AvailableTo = new DateTime(2026, 12, 3, 16, 0, 0) },
                    new ResourceViewModel { Name = "Båt" }
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
            Assert.Empty(repo.Resources);
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
            var controller = new ResourceController(repo, new FakeAllocationRepository(), new FakeUserRepository(), new FakeNeedRepository(), new FakeNotificationRepository());

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
            var controller = new ResourceController(repo, new FakeAllocationRepository(), new FakeUserRepository(), new FakeNeedRepository(), new FakeNotificationRepository());
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
            var controller = new ResourceController(repo, new FakeAllocationRepository(), new FakeUserRepository(), new FakeNeedRepository(), new FakeNotificationRepository());
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
            var controller = new ResourceController(repo, new FakeAllocationRepository(), new FakeUserRepository(), new FakeNeedRepository(), new FakeNotificationRepository());
            var model = new ResourceViewModel { Id = 99, Name = "Aggregat", Latitude = 58.1467, Longitude = 7.9956, AvailableFrom = new DateTime(2026, 12, 1, 8, 0, 0), AvailableTo = new DateTime(2026, 12, 3, 16, 0, 0) };

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
                Longitude = 10.7,
                AvailableFrom = new DateTime(2026, 12, 1, 8, 0, 0),
                AvailableTo = new DateTime(2026, 12, 3, 16, 0, 0)
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
        public async Task Edit_Post_QuantityBelowAllocated_ReturnsViewWithErrorAndKeepsQuantity()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 1, Name = "Aggregat", Quantity = 2, IsAvailable = false, Coords = new Coordinates(58.1467, 7.9956) });
            var allocations = new FakeAllocationRepository();
            await allocations.AddAsync(new Allocation { NeedId = 1, ResourceId = 1, Quantity = 2 });
            var controller = CreateController(repo, isAdmin: true, allocations: allocations);
            var model = new ResourceViewModel
            {
                Id = 1,
                Name = "Aggregat",
                Quantity = 1,
                Availability = true,
                Latitude = 58.1467,
                Longitude = 7.9956,
                AvailableFrom = new DateTime(2026, 12, 1, 8, 0, 0),
                AvailableTo = new DateTime(2026, 12, 3, 16, 0, 0)
            };

            // Act
            var result = await controller.Edit(1, model);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(2, Assert.IsType<ResourceViewModel>(view.Model).Allocated);
            Assert.True(controller.ModelState.ContainsKey(nameof(ResourceViewModel.Quantity)));
            var resource = Assert.Single(repo.Resources);
            Assert.Equal(2, resource.Quantity);
            Assert.False(resource.IsAvailable);
        }

        [Theory]
        [InlineData(2, false)] // Alt er tildelt: ikke ledig selv om det er krysset av
        [InlineData(3, true)]  // Én er ledig
        public async Task Edit_Post_WithAllocations_AvailabilityFollowsWhatIsLeft(int quantity, bool expectedAvailable)
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 1, Name = "Aggregat", Quantity = 2, IsAvailable = false, Coords = new Coordinates(58.1467, 7.9956) });
            var allocations = new FakeAllocationRepository();
            await allocations.AddAsync(new Allocation { NeedId = 1, ResourceId = 1, Quantity = 2 });
            var controller = CreateController(repo, isAdmin: true, allocations: allocations);
            var model = new ResourceViewModel
            {
                Id = 1,
                Name = "Aggregat",
                Quantity = quantity,
                Availability = true,
                Latitude = 58.1467,
                Longitude = 7.9956,
                AvailableFrom = new DateTime(2026, 12, 1, 8, 0, 0),
                AvailableTo = new DateTime(2026, 12, 3, 16, 0, 0)
            };

            // Act
            var result = await controller.Edit(1, model);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            var resource = Assert.Single(repo.Resources);
            Assert.Equal(quantity, resource.Quantity);
            Assert.Equal(expectedAvailable, resource.IsAvailable);
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
        public async Task Delete_LastAllocatedResource_SetsNeedToUnderReviewAndNotifiesReporter()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 1, Name = "Aggregat", UserId = "user-1", Coords = new Coordinates(58.1467, 7.9956) });
            var needs = new FakeNeedRepository();
            needs.Needs.Add(new Need { Id = 10, Name = "Strøm", Status = EntryStatus.Assigned, UserId = "koordinator-1", Coords = new Coordinates(58.1467, 7.9956) });
            var allocations = new FakeAllocationRepository();
            await allocations.AddAsync(new Allocation { NeedId = 10, ResourceId = 1, Quantity = 1 });
            var notifications = new FakeNotificationRepository();
            var controller = CreateController(repo, allocations: allocations, needs: needs, notifications: notifications);

            // Act
            await controller.Delete(1);

            // Assert
            Assert.Empty(await allocations.GetAllAsync());
            Assert.Equal(EntryStatus.UnderReview, needs.Needs.Single().Status);
            var varsel = Assert.Single(notifications.Notifications);
            Assert.Equal("koordinator-1", varsel.UserId);
        }

        [Fact]
        public async Task Delete_ResourceWhenNeedHasOtherAllocations_KeepsNeedAssigned()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource { Id = 1, Name = "Aggregat", UserId = "user-1", Coords = new Coordinates(58.1467, 7.9956) });
            repo.Resources.Add(new Resource { Id = 2, Name = "Kabel", UserId = "user-2", Coords = new Coordinates(58.1467, 7.9956) });
            var needs = new FakeNeedRepository();
            needs.Needs.Add(new Need { Id = 10, Name = "Strøm", Status = EntryStatus.Assigned, UserId = "koordinator-1", Coords = new Coordinates(58.1467, 7.9956) });
            var allocations = new FakeAllocationRepository();
            await allocations.AddAsync(new Allocation { NeedId = 10, ResourceId = 1, Quantity = 1 });
            await allocations.AddAsync(new Allocation { NeedId = 10, ResourceId = 2, Quantity = 1 });
            var notifications = new FakeNotificationRepository();
            var controller = CreateController(repo, allocations: allocations, needs: needs, notifications: notifications);

            // Act
            await controller.Delete(1);

            // Assert
            Assert.Single(await allocations.GetAllAsync());
            Assert.Equal(EntryStatus.Assigned, needs.Needs.Single().Status);
            Assert.Single(notifications.Notifications);
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
                    new ResourceViewModel { Name = "Aggregat", AvailableFrom = new DateTime(2026, 12, 1, 8, 0, 0), AvailableTo = new DateTime(2026, 12, 3, 16, 0, 0) },
                    new ResourceViewModel { Name = "Båt", AvailableFrom = new DateTime(2026, 12, 1, 8, 0, 0), AvailableTo = new DateTime(2026, 12, 3, 16, 0, 0) }
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
                        LicensePlate = "AB12345",
                        AvailableFrom = new DateTime(2026, 12, 1, 8, 0, 0),
                        AvailableTo = new DateTime(2026, 12, 3, 16, 0, 0)
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
                        Size = "L",
                        AvailableFrom = new DateTime(2026, 12, 1, 8, 0, 0),
                        AvailableTo = new DateTime(2026, 12, 3, 16, 0, 0)
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
        public async Task Edit_Get_Competence_ExtractsYrkeAndSetsCompetenceType()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource
            {
                Id = 1,
                Name = "Sykepleier",
                Description = "Ti års erfaring [Yrke: Health]",
                Category = "Competence",
                SubCategory = "Health",
                Coords = new Coordinates(58.1467, 7.9956)
            });
            var controller = CreateController(repo, isAdmin: true);

            // Act
            var result = await controller.Edit(1);

            // Assert
            var model = Assert.IsType<ResourceViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal("Ti års erfaring", model.Description);
            Assert.Equal("Health", model.CompetenceType);
            Assert.Equal(ResourceViewModel.ResourceType.Competence, model.Category);
        }

        [Fact]
        public async Task Edit_Competence_GetThenPost_KeepsSubCategoryAndSingleYrkeTag()
        {
            // Arrange
            var repo = new FakeResourceRepository();
            repo.Resources.Add(new Resource
            {
                Id = 1,
                Name = "Sykepleier",
                Description = "Ti års erfaring [Yrke: Health]",
                Category = "Competence",
                SubCategory = "Health",
                Quantity = 1,
                Coords = new Coordinates(58.1467, 7.9956)
            });
            var controller = CreateController(repo, isAdmin: true);

            // Act: lagre skjemaet uendret
            var model = Assert.IsType<ResourceViewModel>(Assert.IsType<ViewResult>(await controller.Edit(1)).Model);
            await controller.Edit(1, model with
            {
                AvailableFrom = new DateTime(2026, 12, 1, 8, 0, 0),
                AvailableTo = new DateTime(2026, 12, 3, 16, 0, 0)
            });

            // Assert
            var saved = Assert.Single(repo.Resources);
            Assert.Equal("Health", saved.SubCategory);
            Assert.Equal("Ti års erfaring [Yrke: Health]", saved.Description);
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
            var controller = new ResourceController(repo, new FakeAllocationRepository(), new FakeUserRepository(), new FakeNeedRepository(), new FakeNotificationRepository());
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
            var controller = new ResourceController(repo, new FakeAllocationRepository(), new FakeUserRepository(), new FakeNeedRepository(), new FakeNotificationRepository());
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
                Longitude = 7.9956,
                AvailableFrom = new DateTime(2026, 12, 1, 8, 0, 0),
                AvailableTo = new DateTime(2026, 12, 3, 16, 0, 0)
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
                Longitude = 7.9956,
                AvailableFrom = new DateTime(2026, 12, 1, 8, 0, 0),
                AvailableTo = new DateTime(2026, 12, 3, 16, 0, 0)
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
            var features = GetFeatures(result);
            Assert.Equal(2, features.Count);

            var aggregat = features.Single(f => (string)f.Attributes["name"] == "Aggregat");
            Assert.Equal(ResourceViewModel.ResourceType.Tool, (ResourceViewModel.ResourceType)aggregat.Attributes["category"]);
            Assert.Equal(2, (int)aggregat.Attributes["quantity"]);
            Assert.False((bool)aggregat.Attributes["availability"]);

            // X er lengdegrad, Y er breddegrad
            var punkt = Assert.IsType<NtsPoint>(aggregat.Geometry);
            Assert.Equal(58.1467, punkt.Y);
            Assert.Equal(7.9956, punkt.X);
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
            var resource = Assert.Single(GetFeatures(result));
            Assert.Equal(ResourceViewModel.ResourceType.Other, (ResourceViewModel.ResourceType)resource.Attributes["category"]);
        }
    }
}