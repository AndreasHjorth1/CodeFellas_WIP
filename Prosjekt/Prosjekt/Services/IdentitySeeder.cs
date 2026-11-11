using Microsoft.AspNetCore.Identity;
using Prosjekt.Models.Authorization;
using Prosjekt.Models.Entities;

namespace Prosjekt.Services
{
    /// <summary>
    /// Oppretter rollene og tre testbrukere ved oppstart, hvis de ikke finnes.
    /// Testbrukerne har ikke passord før det settes med «Glemt passord».
    /// </summary>
    public class IdentitySeeder
    {
        public const string DevelopmentAdminEmail = "admin@heimevernet.local";
        public const string DevelopmentUserEmail = "user@heimevernet.local";
        public const string DevelopmentCoordinatorEmail = "koordinator@heimevernet.local";

        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var role in new[] { RoleNames.Admin, RoleNames.Coordinator })
            {
                if (await roleManager.RoleExistsAsync(role))
                {
                    continue;
                }

                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Could not create the {role} role: {string.Join(", ", result.Errors.Select(error =>
        error.Description))}");
                }
            }
        }

        public static async Task SeedDevelopmentAdminAsync(UserManager<ApplicationUser> userManager)
        {
            var user = await FindOrCreateDevelopmentUserAsync(userManager, DevelopmentAdminEmail);

            if (!await userManager.IsInRoleAsync(user, RoleNames.Admin))
            {
                await EnsureSucceededAsync(
                    userManager.AddToRoleAsync(user, RoleNames.Admin),
                    $"Could not add the development administrator to the {RoleNames.Admin} role.");
            }
        }

        public static async Task SeedDevelopmentCoordinatorAsync(UserManager<ApplicationUser> userManager)
        {
            var user = await FindOrCreateDevelopmentUserAsync(userManager, DevelopmentCoordinatorEmail);

            if (!await userManager.IsInRoleAsync(user, RoleNames.Coordinator))
            {
                await EnsureSucceededAsync(
                    userManager.AddToRoleAsync(user, RoleNames.Coordinator),
                    $"Could not add the development coordinator to the {RoleNames.Coordinator} role.");
            }
        }

        public static async Task<ApplicationUser> SeedDevelopmentUserAsync(UserManager<ApplicationUser> userManager)
        {
            return await FindOrCreateDevelopmentUserAsync(userManager, DevelopmentUserEmail);
        }

        private static async Task<ApplicationUser> FindOrCreateDevelopmentUserAsync(
            UserManager<ApplicationUser> userManager,
            string email)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = DevelopmentName(email),
                    EmailConfirmed = true
                };

                await EnsureSucceededAsync(
                    userManager.CreateAsync(user),
                    "Could not create the development user.");
            }

            return user;
        }

        private static string DevelopmentName(string email) => email switch
        {
            DevelopmentAdminEmail => "Test Admin",
            DevelopmentCoordinatorEmail => "Test Koordinator",
            _ => "Test Bruker"
        };

        private static async Task EnsureSucceededAsync(Task<IdentityResult> operation, string failureMessage)
        {
            var result = await operation;
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"{failureMessage} {string.Join(", ", result.Errors.Select(error => error.Description))}");
            }
        }
    }
}
