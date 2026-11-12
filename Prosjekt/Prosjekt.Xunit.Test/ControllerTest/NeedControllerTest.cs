using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NetTopologySuite.Features;
using Prosjekt.Controllers;
using Prosjekt.Models.Authorization;
using Prosjekt.Models.Entities;
using Prosjekt.Models.Enums;
using Prosjekt.Models.ModelView;
using Prosjekt.Xunit.Test.Fakes;
using System.Security.Claims;
using NtsPoint = NetTopologySuite.Geometries.Point;
using NtsPolygon = NetTopologySuite.Geometries.Polygon;

namespace Prosjekt.Xunit.Test.ControllerTest
{
    public class NeedControllerTest
    {
        private static FeatureCollection GetFeatures(IActionResult result)
        {
            var jsonResult = Assert.IsType<JsonResult>(result);
            return Assert.IsType<FeatureCollection>(jsonResult.Value);
        }

        private static NeedController CreateSignedInController(FakeNeedRepository repo, bool isAdmin)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user-1") };
            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, RoleNames.Admin));
            }

            return new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), new FakeUserRepository())
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                    }
                }
            };
        }

        [Fact]
        public void Index_ReturnsViewWithOneEmptyNeed()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            var controller = new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), new FakeUserRepository());

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
            var controller = new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), new FakeUserRepository());

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
            var controller = new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), new FakeUserRepository());

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
            var controller = new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), new FakeUserRepository());

            var model = new NeedCreatorViewModel
            {
                NeedList = new List<NeedViewModel>
                {
                    new NeedViewModel { Name = "" }
                },
                Latitude = 58.1467,
                Longitude = 7.9956,
                Deadline = new DateTime(2026, 12, 1, 12, 0, 0), Importance = ImportanceLevel.Planned
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
            var controller = CreateSignedInController(repo, isAdmin: true);

            var model = new NeedCreatorViewModel
            {
                NeedList = new List<NeedViewModel>
                {
                    new NeedViewModel { Name = "Vann" }
                },
                Latitude = 58.1467,
                Longitude = 7.9956,
                Deadline = new DateTime(2026, 12, 1, 12, 0, 0), Importance = ImportanceLevel.Planned
            };

            // Act
            var result = await controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Create",viewResult.ViewName);

            var savedNeed = Assert.Single(repo.Needs);
            Assert.Equal("Vann", savedNeed.Name);
            Assert.Equal("user-1", savedNeed.UserId);
        }

        [Fact]
        public async Task Create_MissingDeadlineOnRow_StillSavesNeed()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            var controller = CreateSignedInController(repo, isAdmin: true);
            controller.ModelState.AddModelError("NeedList[0].Deadline", "Required");

            var model = new NeedCreatorViewModel
            {
                NeedList = new List<NeedViewModel>
                {
                    new NeedViewModel { Name = "Vann" }
                },
                Latitude = 58.1467,
                Longitude = 7.9956,
                Deadline = new DateTime(2026, 12, 1, 12, 0, 0), Importance = ImportanceLevel.Planned
            };

            // Act
            var result = await controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Create", viewResult.ViewName);
            Assert.Single(repo.Needs);
        }

        [Fact]
        public async Task Create_MissingDeadline_ReturnsViewWithErrorAndSavesNothing()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            var controller = CreateSignedInController(repo, isAdmin: true);

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
            Assert.Equal("Index", viewResult.ViewName);
            Assert.True(controller.ModelState.ContainsKey(nameof(NeedCreatorViewModel.Deadline)));
            Assert.Empty(repo.Needs);
        }

        [Fact]
        public async Task Create_UndefinedImportance_ReturnsViewWithErrorAndSavesNothing()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            var controller = CreateSignedInController(repo, isAdmin: true);

            var model = new NeedCreatorViewModel
            {
                NeedList = new List<NeedViewModel>
                {
                    new NeedViewModel { Name = "Vann" }
                },
                Latitude = 58.1467,
                Longitude = 7.9956,
                Deadline = new DateTime(2026, 12, 1, 12, 0, 0),
                Importance = ImportanceLevel.Undefined
            };

            // Act
            var result = await controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.True(controller.ModelState.ContainsKey(nameof(NeedCreatorViewModel.Importance)));
            Assert.Empty(repo.Needs);
        }

        [Fact]
        public async Task Edit_Post_UndefinedImportance_ReturnsViewWithErrorAndKeepsPriority()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            repo.Needs.Add(new Need { Id = 1, Name = "Vann", Priority = "Acute", Coords = new Coordinates(58.1467, 7.9956) });
            var controller = new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), new FakeUserRepository());
            var model = new NeedViewModel
            {
                Id = 1,
                Name = "Vann",
                Latitude = 58.1467,
                Longitude = 7.9956,
                Deadline = new DateTime(2026, 12, 1, 12, 0, 0)
            };

            // Act
            var result = await controller.Edit(1, model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.True(controller.ModelState.ContainsKey(nameof(NeedViewModel.Importance)));
            Assert.Equal("Acute", repo.Needs.Single().Priority);
        }

        [Fact]
        public async Task Create_TwoNeeds_AddsBothToRepository()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            var controller = CreateSignedInController(repo, isAdmin: true);

            var model = new NeedCreatorViewModel
            {
                NeedList = new List<NeedViewModel>
                {
                    new NeedViewModel { Name = "Vann" },
                    new NeedViewModel { Name = "Traktor" }
                },
                Latitude = 58.1467,
                Longitude = 7.9956,
                Deadline = new DateTime(2026, 12, 1, 12, 0, 0), Importance = ImportanceLevel.Planned
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
            var controller = new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), new FakeUserRepository());

            // Act
            var result = await controller.Overview();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<NeedViewModel>>(viewResult.Model);
            Assert.Equal(2, model.Count);
            Assert.Contains(model, n => n.Name == "Vann");
            Assert.Contains(model, n => n.Name == "Traktor");
        }

        /// Oversikten viser hvem som meldte behovet. Ukjente brukere gir tomt navn.
        [Fact]
        public async Task Overview_ShowsWhoReportedTheNeed()
        {
            var repo = new FakeNeedRepository();
            repo.Needs.Add(new Need { Id = 1, Name = "Vann", UserId = "koordinator-1", Coords = new Coordinates(58.1467, 7.9956) });
            repo.Needs.Add(new Need { Id = 2, Name = "Traktor", UserId = "slettet-bruker", Coords = new Coordinates(58.1467, 7.9956) });
            var users = new FakeUserRepository();
            users.Names["koordinator-1"] = "Kari Nordmann";
            var controller = new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), users);

            var result = await controller.Overview();

            var model = Assert.IsType<List<NeedViewModel>>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal("Kari Nordmann", model.Single(n => n.Id == 1).ReportedBy);
            Assert.Null(model.Single(n => n.Id == 2).ReportedBy);
        }

        /// Finn ressurser viser hvem som eier hver ressurs
        [Fact]
        public async Task Match_ShowsOwnerName()
        {
            var needs = new FakeNeedRepository();
            needs.Needs.Add(new Need { Id = 1, Name = "Vann", Category = "Other", Coords = new Coordinates(58.1467, 7.9956) });
            var resources = new FakeResourceRepository();
            resources.Resources.Add(new Resource
            {
                Id = 5, Name = "Aggregat", Category = "Tool", IsAvailable = true, Quantity = 1,
                UserId = "user-2", Coords = new Coordinates(58.15, 8.0)
            });
            var users = new FakeUserRepository();
            users.Names["user-2"] = "Ola Nordmann";
            var controller = new NeedController(needs, resources, new FakeAllocationRepository(), new FakeNotificationRepository(), users);

            var result = await controller.Match(1);

            var matches = Assert.IsType<List<ResourceMatchViewModel>>(Assert.IsType<ViewResult>(result).Model);
            Assert.Equal("Ola Nordmann", Assert.Single(matches).OwnerName);
        }

        [Fact]
        public async Task Edit_Get_UnknownId_ReturnsNotFound()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            var controller = new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), new FakeUserRepository());

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
            var controller = new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), new FakeUserRepository());

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
            var controller = new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), new FakeUserRepository());
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
            var controller = new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), new FakeUserRepository());
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
            var controller = new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), new FakeUserRepository());
            var model = new NeedViewModel { Id = 99, Name = "Vann", Latitude = 58.1467, Longitude = 7.9956, Deadline = new DateTime(2026, 12, 1, 12, 0, 0), Importance = ImportanceLevel.Planned };

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
            var controller = new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), new FakeUserRepository());
            var model = new NeedViewModel
            {
                Id = 1,
                Name = "Mat",
                Status = EntryStatus.UnderReview,
                Latitude = 59.9,
                Longitude = 10.7,
                Deadline = new DateTime(2026, 12, 1, 12, 0, 0), Importance = ImportanceLevel.Planned
            };

            // Act
            var result = await controller.Edit(1, model);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Overview", redirect.ActionName);

            var updatedNeed = Assert.Single(repo.Needs);
            Assert.Equal("Mat", updatedNeed.Name);
            Assert.Equal(EntryStatus.UnderReview, updatedNeed.Status);
            Assert.Equal(59.9, updatedNeed.Coords.Latitude);
        }

        [Fact]
        public async Task Delete_RemovesNeedAndRedirectsToOverview()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            repo.Needs.Add(new Need { Id = 1, Name = "Vann", Coords = new Coordinates(58.1467, 7.9956) });
            var controller = new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), new FakeUserRepository());

            // Act
            var result = await controller.Delete(1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Overview", redirect.ActionName);
            Assert.Empty(repo.Needs);
        }

        [Fact]
        public async Task Edit_Post_SkipsStatusStep_ReturnsViewWithErrorAndKeepsStatus()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            repo.Needs.Add(new Need
            {
                Id = 1,
                Name = "Vann",
                Status = EntryStatus.New,
                Coords = new Coordinates(58.1467, 7.9956)
            });
            var controller = new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), new FakeUserRepository());
            var model = new NeedViewModel
            {
                Id = 1,
                Name = "Vann",
                Status = EntryStatus.Resolved, // Hopper over Vurderes og Tildelt
                Latitude = 58.1467,
                Longitude = 7.9956,
                Deadline = new DateTime(2026, 12, 1, 12, 0, 0), Importance = ImportanceLevel.Planned
            };

            // Act
            var result = await controller.Edit(1, model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(nameof(NeedViewModel.Status)));

            var need = Assert.Single(repo.Needs);
            Assert.Equal(EntryStatus.New, need.Status);
        }

        [Fact]
        public async Task Edit_Post_OneStatusStepBack_UpdatesStatusAndRedirects()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            repo.Needs.Add(new Need
            {
                Id = 1,
                Name = "Vann",
                Status = EntryStatus.Assigned,
                Coords = new Coordinates(58.1467, 7.9956)
            });
            var controller = new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), new FakeUserRepository());
            var model = new NeedViewModel
            {
                Id = 1,
                Name = "Vann",
                Status = EntryStatus.UnderReview, // Ett steg tilbake (angre en feil)
                Latitude = 58.1467,
                Longitude = 7.9956,
                Deadline = new DateTime(2026, 12, 1, 12, 0, 0), Importance = ImportanceLevel.Planned
            };

            // Act
            var result = await controller.Edit(1, model);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Overview", redirect.ActionName);

            var need = Assert.Single(repo.Needs);
            Assert.Equal(EntryStatus.UnderReview, need.Status);
        }

        [Fact]
        public async Task Create_NullNeedList_ReturnsIndexViewWithError()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            var controller = new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), new FakeUserRepository());

            var model = new NeedCreatorViewModel
            {
                NeedList = null!,
                Latitude = 58.1467,
                Longitude = 7.9956,
                Deadline = new DateTime(2026, 12, 1, 12, 0, 0), Importance = ImportanceLevel.Planned
            };

            // Act
            var result = await controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Single(model.NeedList);
            Assert.False(controller.ModelState.IsValid);
            Assert.Empty(repo.Needs);
        }

        [Fact]
        public async Task Create_InvalidModelState_ReturnsIndexView()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            var controller = new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), new FakeUserRepository());
            controller.ModelState.AddModelError("NeedList[0].Phone", "Required");

            var model = new NeedCreatorViewModel
            {
                NeedList = new List<NeedViewModel>
                  {
                      new NeedViewModel { Name = "Vann" }
                  },
                Latitude = 58.1467,
                Longitude = 7.9956,
                Deadline = new DateTime(2026, 12, 1, 12, 0, 0), Importance = ImportanceLevel.Planned
            };

            // Act
            var result = await controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Empty(repo.Needs);
        }

        [Fact]
        public async Task Edit_Post_InvalidCoordinates_ReturnsViewWithError()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            repo.Needs.Add(new Need
            {
                Id = 1,
                Name = "Vann",
                Coords = new Coordinates(58.1467,
7.9956)
            });
            var controller = new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), new FakeUserRepository());
            var model = new NeedViewModel { Id = 1, Name = "Mat", Latitude = 200, Longitude = 10 };

            // Act
            var result = await controller.Edit(1, model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.False(controller.ModelState.IsValid);
            Assert.Equal("Vann", repo.Needs.Single().Name);
        }

        [Fact]
        public async Task Edit_Post_InvalidModelState_ReturnsView()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            repo.Needs.Add(new Need
            {
                Id = 1,
                Name = "Vann",
                Coords = new Coordinates(58.1467,
7.9956)
            });
            var controller = new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), new FakeUserRepository());
            controller.ModelState.AddModelError("Phone", "Required");
            var model = new NeedViewModel
            {
                Id = 1,
                Name = "Mat",
                Latitude = 58.1467,
                Longitude =
7.9956
            };

            // Act
            var result = await controller.Edit(1, model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.Equal("Vann", repo.Needs.Single().Name);
        }

        [Fact]
        public async Task GetInformation_Admin_ReturnsJsonWithAllNeeds()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            repo.Needs.Add(new Need
            {
                Id = 1,
                Name = "Vann",
                Category = "Food",
                Priority = "Acute",
                Status = EntryStatus.Assigned,
                Coords = new Coordinates(58.1467, 7.9956)
            });
            repo.Needs.Add(new Need
            {
                Id = 2,
                Name = "Traktor",
                Coords = new Coordinates(59.9, 10.7)
            });
            var controller = CreateSignedInController(repo, isAdmin: true);

            // Act
            var result = await controller.GetInformation();

            // Assert
            var features = GetFeatures(result);
            Assert.Equal(2, features.Count);

            var vann = features.Single(f => (string)f.Attributes["name"] == "Vann");
            Assert.Equal(NeedViewModel.NeedType.Food, (NeedViewModel.NeedType)vann.Attributes["type"]);
            Assert.Equal(ImportanceLevel.Acute, (ImportanceLevel)vann.Attributes["importance"]);
            Assert.Equal(EntryStatus.Assigned, (EntryStatus)vann.Attributes["status"]);

            // X er lengdegrad, Y er breddegrad
            var punkt = Assert.IsType<NtsPoint>(vann.Geometry);
            Assert.Equal(58.1467, punkt.Y);
            Assert.Equal(7.9956, punkt.X);
        }

        [Fact]
        public async Task GetInformation_IncludesDeadlineInNorwegianTimeAndAllocatedResources()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            repo.Needs.Add(new Need
            {
                Id = 1,
                Name = "Vann",
                NeededBy = new DateTime(2026, 11, 18, 15, 0, 0, DateTimeKind.Utc),
                Coords = new Coordinates(58.1467, 7.9956)
            });
            repo.Needs.Add(new Need
            {
                Id = 2,
                Name = "Traktor",
                Coords = new Coordinates(59.9, 10.7)
            });

            // To tildelinger av samme ressurs teller som én ressurs
            var allocations = new FakeAllocationRepository();
            await allocations.AddAsync(new Allocation { NeedId = 1, ResourceId = 10 });
            await allocations.AddAsync(new Allocation { NeedId = 1, ResourceId = 10 });
            await allocations.AddAsync(new Allocation { NeedId = 1, ResourceId = 11 });

            var controller = new NeedController(repo, new FakeResourceRepository(), allocations, new FakeNotificationRepository(), new FakeUserRepository())
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "user-1"),
                            new Claim(ClaimTypes.Role, RoleNames.Admin)
                        }, "Test"))
                    }
                }
            };

            // Act
            var result = await controller.GetInformation();

            // Assert
            var features = GetFeatures(result);
            var vann = features.Single(f => (string)f.Attributes["name"] == "Vann");
            var traktor = features.Single(f => (string)f.Attributes["name"] == "Traktor");

            // 15:00 UTC er 16:00 norsk vintertid
            Assert.Equal("2026-11-18T16:00", vann.Attributes["deadline"]);
            Assert.Equal(2, vann.Attributes["allocatedResources"]);

            // Uten frist blir feltet tomt, og uten tildelinger er antallet 0
            Assert.Equal(string.Empty, traktor.Attributes["deadline"]);
            Assert.Equal(0, traktor.Attributes["allocatedResources"]);
        }

        [Fact]
        public async Task GetInformation_UnknownCategoryAndPriority_UsesOtherAndPlanned()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            repo.Needs.Add(new Need
            {
                Id = 1,
                Name = "Vann",
                Category = "FinnesIkke",
                Priority = "FinnesIkke",
                Coords = new Coordinates(58.1467, 7.9956)
            });
            var controller = CreateSignedInController(repo, isAdmin: true);

            // Act
            var result = await controller.GetInformation();

            // Assert
            var need = Assert.Single(GetFeatures(result));
            Assert.Equal(NeedViewModel.NeedType.Other, (NeedViewModel.NeedType)need.Attributes["type"]);
            Assert.Equal(ImportanceLevel.Planned, (ImportanceLevel)need.Attributes["importance"]);
        }

        [Fact]
        public async Task GetInformation_NotSignedIn_ReturnsChallenge()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            repo.Needs.Add(new Need { Id = 1, Name = "Vann", Coords = new Coordinates(58.1467, 7.9956) });
            var controller = new NeedController(repo, new FakeResourceRepository(), new FakeAllocationRepository(), new FakeNotificationRepository(), new FakeUserRepository())
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

            // Act
            var result = await controller.GetInformation();

            // Assert
            Assert.IsType<ChallengeResult>(result);
        }

        [Fact]
        public async Task GetInformation_RegularUser_HidesPhoneButKeepsArea()
        {
            // Arrange
            // Leaflet lagrer området som en hel Feature
            const string area = "{\"type\":\"Feature\",\"properties\":{},\"geometry\":{\"type\":\"Polygon\",\"coordinates\":[[[7.99,58.14],[8.00,58.14],[8.00,58.15],[7.99,58.14]]]}}";
            var repo = new FakeNeedRepository();
            repo.Needs.Add(new Need { Id = 1, Name = "Vann", PhoneNumber = "12345678", AreaGeoJson = area, Status = EntryStatus.Resolved, Coords = new Coordinates(58.1467, 7.9956) });
            var controller = CreateSignedInController(repo, isAdmin: false);

            // Act
            var result = await controller.GetInformation();

            // Assert
            var need = Assert.Single(GetFeatures(result));
            Assert.IsType<NtsPolygon>(need.Geometry);
            Assert.Equal(58.1467, (double)need.Attributes["latitude"]);
            Assert.Equal(7.9956, (double)need.Attributes["longitude"]);
            Assert.Equal(string.Empty, need.Attributes["phone"]);
        }

        [Fact]
        public async Task GetInformation_AreaAsPlainGeometry_IsAlsoIncluded()
        {
            // Arrange
            const string area = "{\"type\":\"Polygon\",\"coordinates\":[[[7.99,58.14],[8.00,58.14],[8.00,58.15],[7.99,58.14]]]}";
            var repo = new FakeNeedRepository();
            repo.Needs.Add(new Need { Id = 1, Name = "Vann", AreaGeoJson = area, Coords = new Coordinates(58.1467, 7.9956) });
            var controller = CreateSignedInController(repo, isAdmin: true);

            // Act
            var result = await controller.GetInformation();

            // Assert
            var need = Assert.Single(GetFeatures(result));
            Assert.IsType<NtsPolygon>(need.Geometry);
        }

        [Fact]
        public async Task GetInformation_InvalidArea_ReturnsOnlyPoint()
        {
            // Arrange
            var repo = new FakeNeedRepository();
            repo.Needs.Add(new Need { Id = 1, Name = "Vann", AreaGeoJson = "ikke gyldig json", Coords = new Coordinates(58.1467, 7.9956) });
            var controller = CreateSignedInController(repo, isAdmin: true);

            // Act
            var result = await controller.GetInformation();

            // Assert
            var need = Assert.Single(GetFeatures(result));
            Assert.IsType<NtsPoint>(need.Geometry);
        }

        [Fact]
        public async Task Allocate_UnavailableResource_AddsNothingAndShowsError()
        {
            // Arrange
            var needs = new FakeNeedRepository();
            needs.Needs.Add(new Need { Id = 1, Name = "Vann", Coords = new Coordinates(58.1467, 7.9956) });
            var resources = new FakeResourceRepository();
            resources.Resources.Add(new Resource { Id = 1, Name = "Aggregat", Quantity = 2, IsAvailable = false, Coords = new Coordinates(58.1467, 7.9956) });
            var allocations = new FakeAllocationRepository();
            var controller = new NeedController(needs, resources, allocations, new FakeNotificationRepository(), new FakeUserRepository())
            {
                TempData = new TempDataDictionary(new DefaultHttpContext(), new EmptyTempDataProvider())
            };

            // Act
            var result = await controller.Allocate(1, 1, 1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Match", redirect.ActionName);
            Assert.Empty(await allocations.GetAllAsync());
            Assert.Equal(EntryStatus.New, needs.Needs.Single().Status);
            Assert.NotNull(controller.TempData["Error"]);
        }

        [Fact]
        public async Task Allocate_AvailableResource_AddsAllocationAndAssignsNeed()
        {
            // Arrange
            var needs = new FakeNeedRepository();
            needs.Needs.Add(new Need { Id = 1, Name = "Vann", Coords = new Coordinates(58.1467, 7.9956) });
            var resources = new FakeResourceRepository();
            resources.Resources.Add(new Resource { Id = 1, Name = "Aggregat", Quantity = 2, IsAvailable = true, Coords = new Coordinates(58.1467, 7.9956) });
            var allocations = new FakeAllocationRepository();
            var controller = new NeedController(needs, resources, allocations, new FakeNotificationRepository(), new FakeUserRepository());

            // Act
            await controller.Allocate(1, 1, 1);

            // Assert
            Assert.Single(await allocations.GetAllAsync());
            Assert.Equal(EntryStatus.Assigned, needs.Needs.Single().Status);
        }

        [Fact]
        public async Task Deallocate_AllocationBelongsToOtherNeed_ReturnsNotFoundAndChangesNothing()
        {
            // Arrange
            var needs = new FakeNeedRepository();
            needs.Needs.Add(new Need { Id = 1, Name = "Vann", Status = EntryStatus.Assigned, Coords = new Coordinates(58.1467, 7.9956) });
            needs.Needs.Add(new Need { Id = 2, Name = "Traktor", Status = EntryStatus.Assigned, Coords = new Coordinates(59.9, 10.7) });
            var allocations = new FakeAllocationRepository();
            await allocations.AddAsync(new Allocation { NeedId = 1, ResourceId = 1, Quantity = 1 });
            var allocationId = (await allocations.GetAllAsync()).Single().Id;
            var controller = new NeedController(needs, new FakeResourceRepository(), allocations, new FakeNotificationRepository(), new FakeUserRepository());

            // Act
            var result = await controller.Deallocate(allocationId, needId: 2);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            Assert.Single(await allocations.GetAllAsync());
            Assert.All(needs.Needs, n => Assert.Equal(EntryStatus.Assigned, n.Status));
        }

        [Fact]
        public async Task Deallocate_LastAllocation_RemovesItAndMovesNeedToUnderReview()
        {
            // Arrange
            var needs = new FakeNeedRepository();
            needs.Needs.Add(new Need { Id = 1, Name = "Vann", Status = EntryStatus.Assigned, Coords = new Coordinates(58.1467, 7.9956) });
            var allocations = new FakeAllocationRepository();
            await allocations.AddAsync(new Allocation { NeedId = 1, ResourceId = 1, Quantity = 1 });
            var allocationId = (await allocations.GetAllAsync()).Single().Id;
            var controller = new NeedController(needs, new FakeResourceRepository(), allocations, new FakeNotificationRepository(), new FakeUserRepository());

            // Act
            var result = await controller.Deallocate(allocationId, needId: 1);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Empty(await allocations.GetAllAsync());
            Assert.Equal(EntryStatus.UnderReview, needs.Needs.Single().Status);
        }

        [Fact]
        public async Task Resolve_AssignedNeed_ReturnsResources()
        {
            // Arrange
            var needs = new FakeNeedRepository();
            needs.Needs.Add(new Need { Id = 1, Name = "Vann", Status = EntryStatus.Assigned, Coords = new Coordinates(58.1467, 7.9956) });
            var resources = new FakeResourceRepository();
            resources.Resources.Add(new Resource { Id = 1, Name = "Aggregat", Quantity = 2, IsAvailable = false, Coords = new Coordinates(58.1467, 7.9956) });
            var allocations = new FakeAllocationRepository();
            await allocations.AddAsync(new Allocation { NeedId = 1, ResourceId = 1, Quantity = 2 });
            var controller = new NeedController(needs, resources, allocations, new FakeNotificationRepository(), new FakeUserRepository());

            // Act
            var result = await controller.Resolve(1);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(EntryStatus.Resolved, needs.Needs.Single().Status);
            Assert.Empty(await allocations.GetByNeedAsync(1));
            Assert.True(resources.Resources.Single().IsAvailable);
        }

        [Fact]
        public async Task Resolve_OnlyReturnsAllocationsForThisNeed()
        {
            // Arrange
            var needs = new FakeNeedRepository();
            needs.Needs.Add(new Need { Id = 1, Name = "Vann", Status = EntryStatus.Assigned, Coords = new Coordinates(58.1467, 7.9956) });
            needs.Needs.Add(new Need { Id = 2, Name = "Strøm", Status = EntryStatus.Assigned, Coords = new Coordinates(59.9, 10.7) });
            var resources = new FakeResourceRepository();
            resources.Resources.Add(new Resource { Id = 1, Name = "Aggregat", Quantity = 2, IsAvailable = false, Coords = new Coordinates(58.1467, 7.9956) });
            var allocations = new FakeAllocationRepository();
            await allocations.AddAsync(new Allocation { NeedId = 1, ResourceId = 1, Quantity = 1 });
            await allocations.AddAsync(new Allocation { NeedId = 2, ResourceId = 1, Quantity = 1 });
            var controller = new NeedController(needs, resources, allocations, new FakeNotificationRepository(), new FakeUserRepository());

            // Act
            await controller.Resolve(1);

            // Assert
            Assert.Empty(await allocations.GetByNeedAsync(1));
            Assert.Single(await allocations.GetByNeedAsync(2));
            // Én av to er ledig igjen, så ressursen er tilgjengelig
            Assert.True(resources.Resources.Single().IsAvailable);
        }

        [Fact]
        public async Task Resolve_NewNeed_KeepsAllocations()
        {
            // Arrange
            var needs = new FakeNeedRepository();
            needs.Needs.Add(new Need { Id = 1, Name = "Vann", Status = EntryStatus.New, Coords = new Coordinates(58.1467, 7.9956) });
            var resources = new FakeResourceRepository();
            resources.Resources.Add(new Resource { Id = 1, Name = "Aggregat", Quantity = 1, IsAvailable = false, Coords = new Coordinates(58.1467, 7.9956) });
            var allocations = new FakeAllocationRepository();
            await allocations.AddAsync(new Allocation { NeedId = 1, ResourceId = 1, Quantity = 1 });
            var controller = new NeedController(needs, resources, allocations, new FakeNotificationRepository(), new FakeUserRepository())
            {
                TempData = new TempDataDictionary(new DefaultHttpContext(), new EmptyTempDataProvider())
            };

            // Act
            var result = await controller.Resolve(1);

            // Assert
            Assert.Equal(EntryStatus.New, needs.Needs.Single().Status);
            Assert.Single(await allocations.GetByNeedAsync(1));
            Assert.False(resources.Resources.Single().IsAvailable);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Match", redirect.ActionName);
            Assert.NotNull(controller.TempData["Error"]);
        }

        [Theory]
        [InlineData(EntryStatus.New)]
        [InlineData(EntryStatus.Assigned)]
        [InlineData(EntryStatus.Resolved)]
        public async Task Match_SetsNeedStatusForButtons(EntryStatus status)
        {
            // Arrange
            var needs = new FakeNeedRepository();
            needs.Needs.Add(new Need { Id = 1, Name = "Vann", Status = status, Coords = new Coordinates(58.1467, 7.9956) });
            var controller = new NeedController(needs, new FakeResourceRepository(), new FakeAllocationRepository(),
                new FakeNotificationRepository(), new FakeUserRepository());

            // Act
            await controller.Match(1);

            // Assert
            Assert.Equal(status, (EntryStatus)controller.ViewBag.NeedStatus);
        }

        [Fact]
        public async Task Allocate_AvailableResource_NotifiesOwner()
        {
            // Arrange
            var needs = new FakeNeedRepository();
            needs.Needs.Add(new Need
            {
                Id = 1,
                Name = "Vann",
                Coords = new Coordinates(58.1467, 7.9956)
            });
            var resources = new FakeResourceRepository();
            resources.Resources.Add(new Resource
            {
                Id = 1,
                Name = "Aggregat",
                Quantity = 2,
                IsAvailable = true,
                UserId = "eier-1",
                Coords = new Coordinates(58.1467, 7.9956)
            });
            var notifications = new FakeNotificationRepository();
            var controller = new NeedController(needs, resources, new FakeAllocationRepository(), notifications, new FakeUserRepository());

            // Act
            await controller.Allocate(1, 1, 1);

            // Assert
            var notification = Assert.Single(notifications.Notifications);
            Assert.Equal("eier-1", notification.UserId);
            Assert.Contains("Aggregat", notification.Message);
            Assert.Contains("Vann", notification.Message);
        }

        [Fact]
        public async Task Allocate_ResourceWithoutOwner_SendsNoNotification()
        {
            // Arrange
            var needs = new FakeNeedRepository();
            needs.Needs.Add(new Need
            {
                Id = 1,
                Name = "Vann",
                Coords = new Coordinates(58.1467, 7.9956)
            });
            var resources = new FakeResourceRepository();
            resources.Resources.Add(new Resource
            {
                Id = 1,
                Name = "Aggregat",
                Quantity = 2,
                IsAvailable = true,
                Coords = new Coordinates(58.1467, 7.9956)
            });
            var notifications = new FakeNotificationRepository();
            var controller = new NeedController(needs, resources, new FakeAllocationRepository(), notifications, new FakeUserRepository());

            // Act
            await controller.Allocate(1, 1, 1);

            // Assert
            Assert.Empty(notifications.Notifications);
        }

        [Fact]
        public async Task Deallocate_NotifiesOwner()
        {
            // Arrange
            var needs = new FakeNeedRepository();
            needs.Needs.Add(new Need
            {
                Id = 1,
                Name = "Vann",
                Status = EntryStatus.Assigned,
                Coords = new Coordinates(58.1467, 7.9956)
            });
            var resources = new FakeResourceRepository();
            resources.Resources.Add(new Resource
            {
                Id = 1,
                Name = "Aggregat",
                Quantity = 1,
                IsAvailable = false,
                UserId = "eier-1",
                Coords = new Coordinates(58.1467, 7.9956)
            });
            var allocations = new FakeAllocationRepository();
            await allocations.AddAsync(new Allocation { NeedId = 1, ResourceId = 1, Quantity = 1 });
            var allocationId = (await allocations.GetAllAsync()).Single().Id;
            var notifications = new FakeNotificationRepository();
            var controller = new NeedController(needs, resources, allocations, notifications, new FakeUserRepository());

            // Act
            await controller.Deallocate(allocationId, needId: 1);

            // Assert
            var notification = Assert.Single(notifications.Notifications);
            Assert.Equal("eier-1", notification.UserId);
            Assert.Contains("levert tilbake", notification.Message);
        }

        [Fact]
        public async Task Resolve_AssignedNeed_NotifiesEachOwner()
        {
            // Arrange
            var needs = new FakeNeedRepository();
            needs.Needs.Add(new Need
            {
                Id = 1,
                Name = "Vann",
                Status = EntryStatus.Assigned,
                Coords = new Coordinates(58.1467, 7.9956)
            });
            var resources = new FakeResourceRepository();
            resources.Resources.Add(new Resource
            {
                Id = 1,
                Name = "Aggregat",
                Quantity = 1,
                IsAvailable = false,
                UserId = "eier-1",
                Coords = new Coordinates(58.1467, 7.9956)
            });
            resources.Resources.Add(new Resource
            {
                Id = 2,
                Name = "Vannkanne",
                Quantity = 1,
                IsAvailable = false,
                UserId = "eier-2",
                Coords = new Coordinates(58.1467, 7.9956)
            });
            var allocations = new FakeAllocationRepository();
            await allocations.AddAsync(new Allocation { NeedId = 1, ResourceId = 1, Quantity = 1 });
            await allocations.AddAsync(new Allocation { NeedId = 1, ResourceId = 2, Quantity = 1 });
            var notifications = new FakeNotificationRepository();
            var controller = new NeedController(needs, resources, allocations, notifications, new FakeUserRepository());

            // Act
            await controller.Resolve(1);

            // Assert
            Assert.Equal(2, notifications.Notifications.Count);
            Assert.Contains(notifications.Notifications, n => n.UserId == "eier-1");
            Assert.Contains(notifications.Notifications, n => n.UserId == "eier-2");
        }

        [Fact]
        public async Task Delete_NeedWithAllocations_NotifiesOwnerAndFreesResource()
        {
            // Arrange
            var needs = new FakeNeedRepository();
            needs.Needs.Add(new Need { Id = 1, Name = "Vann", Status = EntryStatus.Assigned, Coords = new Coordinates(58.1467, 7.9956) });
            var resources = new FakeResourceRepository();
            resources.Resources.Add(new Resource { Id = 1, Name = "Aggregat", Quantity = 1, IsAvailable = false, UserId = "eier-1", Coords = new Coordinates(58.1467, 7.9956) });
            var allocations = new FakeAllocationRepository();
            await allocations.AddAsync(new Allocation { NeedId = 1, ResourceId = 1, Quantity = 1 });
            var notifications = new FakeNotificationRepository();
            var controller = new NeedController(needs, resources, allocations, notifications, new FakeUserRepository());

            // Act
            await controller.Delete(1);

            // Assert
            Assert.Empty(await allocations.GetAllAsync());
            Assert.True(resources.Resources.Single().IsAvailable);
            var notification = Assert.Single(notifications.Notifications);
            Assert.Equal("eier-1", notification.UserId);
            Assert.Contains("slettet", notification.Message);
        }

        [Fact]
        public async Task Delete_NeedWithoutAllocations_SendsNoNotification()
        {
            // Arrange
            var needs = new FakeNeedRepository();
            needs.Needs.Add(new Need { Id = 1, Name = "Vann", Coords = new Coordinates(58.1467, 7.9956) });
            var notifications = new FakeNotificationRepository();
            var controller = new NeedController(needs, new FakeResourceRepository(), new FakeAllocationRepository(), notifications, new FakeUserRepository());

            // Act
            await controller.Delete(1);

            // Assert
            Assert.Empty(notifications.Notifications);
        }

        private sealed class EmptyTempDataProvider : ITempDataProvider
        {
            public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

            public void SaveTempData(HttpContext context, IDictionary<string, object> values)
            {
            }
        }
    }
}