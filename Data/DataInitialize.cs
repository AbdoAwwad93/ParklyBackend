using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parkly_Backend.Data.Repositories;
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
            await SeedAdminNotificationsAsync(sp);
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

        private static async Task SeedAdminNotificationsAsync(IServiceProvider sp)
        {
            try
            {
                var userManager = sp.GetRequiredService<UserManager<AppUser>>();
                var unitOfWork = sp.GetRequiredService<IUnitOfWork>();

                var admins = await userManager.Users.Where(u => u.Role == UserRole.Admin).ToListAsync();
                if (!admins.Any()) return;

                foreach (var admin in admins)
                {
                    var hasNotifications = await unitOfWork.Notifications.AnyAsync(n => n.RecipientUserId == admin.Id);
                    if (hasNotifications) continue;

                    var recentReservations = await unitOfWork.Reservations.Query()
                        .Include(r => r.User)
                        .Include(r => r.ParkingSpace)
                            .ThenInclude(ps => ps.Parking)
                        .ToListAsync();

                    var reservationIds = recentReservations.Select(r => r.ReservationId).ToList();

                    var originalDates = await unitOfWork.Notifications.Query()
                        .Where(n => n.ReservationId != null 
                                 && reservationIds.Contains(n.ReservationId.Value) 
                                 && n.RecipientUserId != admin.Id)
                        .GroupBy(n => n.ReservationId!.Value)
                        .Select(g => new { ReservationId = g.Key, CreatedAt = g.Min(x => x.CreatedAt) })
                        .ToDictionaryAsync(x => x.ReservationId, x => x.CreatedAt);

                    var reservationsWithDates = recentReservations.Select(r =>
                    {
                        var creationDate = originalDates.TryGetValue(r.ReservationId, out var dt)
                            ? dt
                            : (r.ArrivalTime <= DateTime.UtcNow ? r.ArrivalTime : DateTime.UtcNow);
                        return new { Reservation = r, CreatedAt = creationDate };
                    })
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(25)
                    .ToList();

                    foreach (var item in reservationsWithDates)
                    {
                        var r = item.Reservation;
                        var parking = r.ParkingSpace?.Parking;
                        var bookingRef = $"PK-{r.ReservationId.ToString("N")[^4..].ToUpperInvariant()}";
                        var customer = string.IsNullOrWhiteSpace(r.User?.FullName) ? "A customer" : r.User.FullName;

                        await unitOfWork.Notifications.AddAsync(new Notification
                        {
                            RecipientUserId = admin.Id,
                            Type = NotificationType.Booking,
                            Title = $"New Booking — {bookingRef}",
                            Message = $"{customer} reserved spot {r.ParkingSpace?.SpotNumber ?? "spot"} at {parking?.Name ?? "Parking"} for ${r.TotalPrice:F2}.",
                            CreatedAt = item.CreatedAt,
                            IsRead = false,
                            ParkingId = parking?.ParkingId,
                            ReservationId = r.ReservationId,
                            SpaceId = r.SpaceId
                        });
                    }

                    var pendingOwners = await unitOfWork.ParkingOwners.Query()
                        .Include(po => po.User)
                        .Where(po => po.VerificationStatus == VerificationStatus.Pending)
                        .ToListAsync();

                    foreach (var po in pendingOwners)
                    {
                        var ownerName = po.User?.FullName ?? "New Owner";
                        await unitOfWork.Notifications.AddAsync(new Notification
                        {
                            RecipientUserId = admin.Id,
                            Type = NotificationType.Alert,
                            Title = "New Owner Application",
                            Message = $"{po.CompanyName} ({ownerName}) submitted an owner application awaiting verification.",
                            CreatedAt = po.User?.CreatedAt ?? DateTime.UtcNow,
                            IsRead = false
                        });
                    }

                    await unitOfWork.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                var logger = sp.GetRequiredService<ILogger<Program>>();
                logger.LogWarning(ex, "Failed to seed admin notifications.");
            }
        }
    }
}