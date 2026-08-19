using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parkly_Backend.Models;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Data
{
    public static class DataInitialize
    {
        public static async Task InitializeDatabaseAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;

            await SeedRolesAsync(sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>());
            await SeedAdminAsync(sp.GetRequiredService<UserManager<AppUser>>(), sp);
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
        {
            foreach (var roleName in Enum.GetNames<UserRole>())
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                }
            }
        }

        private static async Task SeedAdminAsync(UserManager<AppUser> userManager, IServiceProvider sp)
        {
            if (await userManager.Users.AnyAsync(u => u.Role == UserRole.Admin))
            {
                return;
            }

            var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
            var adminUserName = Environment.GetEnvironmentVariable("ADMIN_USERNAME");
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
            if (string.IsNullOrWhiteSpace(adminEmail)
                || string.IsNullOrWhiteSpace(adminUserName)
                || string.IsNullOrWhiteSpace(adminPassword))
            {
                return;
            }

            var admin = new AppUser
            {
                UserName = adminUserName,
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true,
                Role = UserRole.Admin
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (!result.Succeeded)
            {
                var logger = sp.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Failed to seed admin account: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}