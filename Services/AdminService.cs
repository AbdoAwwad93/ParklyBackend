using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Enums;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Services
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private const decimal PlatformCommissionRate = 0.10m; // 10% platform fee

        public AdminService(
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ApiResponse> RegisterAdmin(RegisterDTO dto)
        {
            var exUser = await _userManager.FindByEmailAsync(dto.Email);
            if (exUser != null)
            {
                return ApiResponse.Failure("This Email is already exists");
            }

            var newUser = _mapper.Map<AppUser>(dto);
            newUser.Role = UserRole.Admin;

            var result = await _userManager.CreateAsync(newUser, dto.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return ApiResponse.Failure("Account creation failed", errors);
            }

            var roleResult = await _userManager.AddToRoleAsync(newUser, UserRole.Admin.ToString());
            if (!roleResult.Succeeded)
            {
                var errors = roleResult.Errors.Select(e => e.Description).ToList();
                return ApiResponse.Failure("Failed to assign Admin role", errors);
            }

            return ApiResponse.Success("Admin account created successfully!");
        }

        public async Task<ApiResponse<AdminDashboardSummaryDTO>> GetSummaryAsync()
        {
            var now = DateTime.UtcNow;
            var todayUtc = now.Date;
            var tomorrowUtc = todayUtc.AddDays(1);
            var thirtyDaysAgo = now.AddDays(-30);

            var allUsers = await _userManager.Users.ToListAsync();
            int registeredDrivers = allUsers.Count(u => u.Role == UserRole.Driver);
            int registeredOwners = allUsers.Count(u => u.Role == UserRole.ParkingOwner);
            var activeDriverIds = await _unitOfWork.Reservations.Query()
                .Where(r => r.ArrivalTime >= thirtyDaysAgo)
                .Select(r => r.UserId)
                .Distinct()
                .CountAsync();

            var allSpaces = await _unitOfWork.ParkingSpaces.GetAllAsync();
            int totalSpaces = allSpaces.Count;
            int activeSpaces = allSpaces.Count(s => s.IsActive);
            double spacesLivePct = totalSpaces > 0
                ? Math.Round(activeSpaces * 100.0 / totalSpaces, 1)
                : 0;
            int activeNow = await _unitOfWork.Reservations.GetPlatformCheckedInCountAsync();
            decimal grossRevenueToday = await _unitOfWork.Reservations.GetPlatformTodayRevenueAsync(todayUtc, tomorrowUtc);
            decimal platformProcessed = Math.Round(grossRevenueToday * PlatformCommissionRate, 2);

            var dto = new AdminDashboardSummaryDTO
            {
                RegisteredDrivers = registeredDrivers,
                ActiveOnPlatform = activeDriverIds,
                RegisteredOwners = registeredOwners,
                TotalSpaces = totalSpaces,
                ActiveNow = activeNow,
                GrossRevenueToday = grossRevenueToday,
                PlatformProcessed = platformProcessed,
                SpacesLivePercent = spacesLivePct
            };

            return ApiResponse<AdminDashboardSummaryDTO>.Success(
                "Admin dashboard summary retrieved successfully.", dto);
        }

        public async Task<ApiResponse<RevenueOverviewDTO>> GetRevenueOverviewAsync(string period)
        {
            var normalizedPeriod = period?.Trim().ToLowerInvariant() ?? "monthly";
            if (normalizedPeriod != "daily" && normalizedPeriod != "weekly" && normalizedPeriod != "monthly")
            {
                return ApiResponse<RevenueOverviewDTO>.Failure("Period must be 'daily', 'weekly', or 'monthly'.");
            }

            var windowStart = normalizedPeriod == "monthly"
                ? DateTime.UtcNow.AddMonths(-12)
                : normalizedPeriod == "weekly"
                    ? DateTime.UtcNow.AddDays(-28)
                    : DateTime.UtcNow.AddDays(-7);

            var transactions = await _unitOfWork.Reservations.GetPlatformRevenueInWindowAsync(windowStart);

            List<RevenueDataPointDTO> dataPoints;
            var now = DateTime.UtcNow;

            if (normalizedPeriod == "daily")
            {
                dataPoints = Enumerable.Range(0, 7)
                    .Select(i =>
                    {
                        var day = now.Date.AddDays(-(6 - i));
                        return new RevenueDataPointDTO
                        {
                            Label = day.ToString("ddd"),
                            Revenue = transactions
                                .Where(t => t.ArrivalTime.Date == day)
                                .Sum(t => t.TotalPrice)
                        };
                    })
                    .ToList();
            }
            else if (normalizedPeriod == "weekly")
            {
                dataPoints = Enumerable.Range(0, 4)
                    .Select(weeksAgo =>
                    {
                        var weekEnd = now.Date.AddDays(-(weeksAgo * 7));
                        var weekStart = weekEnd.AddDays(-6);
                        return new RevenueDataPointDTO
                        {
                            Label = $"Week {4 - weeksAgo}",
                            Revenue = transactions
                                .Where(t => t.ArrivalTime.Date >= weekStart && t.ArrivalTime.Date <= weekEnd)
                                .Sum(t => t.TotalPrice)
                        };
                    })
                    .Reverse()
                    .ToList();
            }
            else
            {
                dataPoints = Enumerable.Range(0, 12)
                    .Select(monthsAgo =>
                    {
                        var month = now.AddMonths(-monthsAgo);
                        var monthStart = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                        var monthEnd = monthStart.AddMonths(1);
                        return new RevenueDataPointDTO
                        {
                            Label = month.ToString("MMM"),
                            Revenue = transactions
                                .Where(t => t.ArrivalTime >= monthStart && t.ArrivalTime < monthEnd)
                                .Sum(t => t.TotalPrice)
                        };
                    })
                    .Reverse()
                    .ToList();
            }

            return ApiResponse<RevenueOverviewDTO>.Success("Revenue overview retrieved successfully.", new RevenueOverviewDTO
            {
                Period = normalizedPeriod,
                TotalRevenue = dataPoints.Sum(dp => dp.Revenue),
                DataPoints = dataPoints
            });
        }

        public async Task<ApiResponse<BookingsChartDTO>> GetBookingsChartAsync(string period)
        {
            var normalizedPeriod = period?.Trim().ToLowerInvariant() ?? "weekly";
            if (normalizedPeriod != "daily" && normalizedPeriod != "weekly")
            {
                return ApiResponse<BookingsChartDTO>.Failure("Period must be 'daily' or 'weekly'.");
            }

            var windowStart = normalizedPeriod == "weekly"
                ? DateTime.UtcNow.AddDays(-28)
                : DateTime.UtcNow.AddDays(-7);

            var bookings = await _unitOfWork.Reservations.GetPlatformBookingsInWindowAsync(windowStart);

            List<BookingsDataPointDTO> dataPoints;
            var now = DateTime.UtcNow;

            if (normalizedPeriod == "daily")
            {
                dataPoints = Enumerable.Range(0, 7)
                    .Select(i =>
                    {
                        var day = now.Date.AddDays(-(6 - i));
                        return new BookingsDataPointDTO
                        {
                            Label = day.ToString("ddd"),
                            Count = bookings.Count(b => b.ArrivalTime.Date == day)
                        };
                    })
                    .ToList();
            }
            else
            {
                dataPoints = Enumerable.Range(0, 4)
                    .Select(weeksAgo =>
                    {
                        var weekEnd = now.Date.AddDays(-(weeksAgo * 7));
                        var weekStart = weekEnd.AddDays(-6);
                        return new BookingsDataPointDTO
                        {
                            Label = $"Week {4 - weeksAgo}",
                            Count = bookings.Count(b => b.ArrivalTime.Date >= weekStart && b.ArrivalTime.Date <= weekEnd)
                        };
                    })
                    .Reverse()
                    .ToList();
            }

            return ApiResponse<BookingsChartDTO>.Success(
                "Bookings chart retrieved successfully.",
                new BookingsChartDTO
                {
                    Period = normalizedPeriod,
                    TotalBookings = dataPoints.Sum(dp => dp.Count),
                    DataPoints = dataPoints
                });
        }

        public async Task<ApiResponse<List<AdminActivityFeedItemDTO>>> GetRecentActivityAsync(int limit)
        {
            limit = Math.Clamp(limit, 1, 50);
            var now = DateTime.UtcNow;
            var since = now.AddDays(-7);

            var feedItems = new List<AdminActivityFeedItemDTO>();
            var recentUsers = await _userManager.Users
                .Where(u => u.CreatedAt >= since)
                .OrderByDescending(u => u.CreatedAt)
                .Take(limit)
                .ToListAsync();

            foreach (var user in recentUsers.Where(u => u.Role == UserRole.Driver))
            {
                feedItems.Add(new AdminActivityFeedItemDTO
                {
                    Type = "NewAccount",
                    Icon = "person_add",
                    Description = $"{user.FullName} created a new account",
                    Timestamp = user.CreatedAt,
                    TimeAgo = FormatTimeAgo(user.CreatedAt, now)
                });
            }
            var recentApplications = await _unitOfWork.ParkingOwners.GetRecentApplicationsAsync(since, limit);
            foreach (var owner in recentApplications)
            {
                var name = owner.User?.FullName ?? "Unknown";
                var createdAt = owner.User?.CreatedAt ?? now;
                feedItems.Add(new AdminActivityFeedItemDTO
                {
                    Type = "OwnerApplication",
                    Icon = "description",
                    Description = $"{name} submitted owner application",
                    Timestamp = createdAt,
                    TimeAgo = FormatTimeAgo(createdAt, now)
                });
            }
            var recentBookings = await _unitOfWork.Reservations.GetRecentPlatformBookingsAsync(since, limit);
            foreach (var reservation in recentBookings)
            {
                var bookingRef = $"RES-{reservation.ReservationId.ToString("N")[^4..].ToUpperInvariant()}";
                var parkingName = reservation.ParkingSpace?.Parking?.Name ?? "Unknown";
                feedItems.Add(new AdminActivityFeedItemDTO
                {
                    Type = "NewBooking",
                    Icon = "calendar_today",
                    Description = $"New booking at {parkingName} — #{bookingRef}",
                    Timestamp = reservation.ArrivalTime,
                    TimeAgo = FormatTimeAgo(reservation.ArrivalTime, now)
                });
            }
            var parkings = await _unitOfWork.Parkings.GetParkingsWithSpacesAsync();
            foreach (var parking in parkings)
            {
                var activeSpaces = parking.ParkingSpaces.Count(s => s.IsActive);
                if (activeSpaces == 0) continue;

                var occupied = await _unitOfWork.Reservations.GetCheckedInCountForParkingAsync(parking.ParkingId);
                var occupancyPct = (int)Math.Round(occupied * 100.0 / activeSpaces);

                if (occupancyPct >= 90)
                {
                    feedItems.Add(new AdminActivityFeedItemDTO
                    {
                        Type = "OccupancyAlert",
                        Icon = "bolt",
                        Description = $"{parking.Name} reached {occupancyPct}% occupancy",
                        Timestamp = now,
                        TimeAgo = "just now"
                    });
                }
            }
            var recentParkings = await _unitOfWork.Parkings.GetRecentParkingsAsync(since, limit);
            foreach (var parking in recentParkings)
            {
                var ownerName = parking.ParkingOwner?.User?.FullName ?? "Unknown";
                feedItems.Add(new AdminActivityFeedItemDTO
                {
                    Type = "NewParking",
                    Icon = "add_circle",
                    Description = $"{ownerName} added {parking.Name}",
                    Timestamp = parking.CreatedAt.GetValueOrDefault(now),
                    TimeAgo = FormatTimeAgo(parking.CreatedAt.GetValueOrDefault(now), now)
                });
            }

            var sortedFeed = feedItems
                .OrderByDescending(f => f.Timestamp)
                .Take(limit)
                .ToList();

            return ApiResponse<List<AdminActivityFeedItemDTO>>.Success(
                "Recent activity retrieved successfully.", sortedFeed);
        }

        public async Task<ApiResponse<List<PendingApprovalDTO>>> GetPendingApprovalsAsync(int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var pendingOwners = await _unitOfWork.ParkingOwners.GetPendingOwnersAsync(page, pageSize);

            var result = pendingOwners.Select(o =>
            {
                var name = o.User?.FullName ?? "Unknown";
                var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var initials = parts.Length >= 2
                    ? $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant()
                    : name.Length > 0 ? name[0].ToString().ToUpperInvariant() : "?";

                return new PendingApprovalDTO
                {
                    OwnerId = o.OwnerId,
                    OwnerName = name,
                    OwnerInitials = initials,
                    CompanyName = o.CompanyName,
                    Location = o.CityStateZip ?? string.Empty,
                    VerificationStatus = o.VerificationStatus.ToString(),
                    AppliedAt = o.User?.CreatedAt ?? DateTime.MinValue
                };
            }).ToList();

            return ApiResponse<List<PendingApprovalDTO>>.Success(
                "Pending approvals retrieved successfully.", result);
        }

        public async Task<ApiResponse> ProcessApprovalAsync(Guid ownerId, string action)
        {
            var owner = await _unitOfWork.ParkingOwners.GetOwnerWithUserAsync(ownerId);
            if (owner == null)
            {
                return ApiResponse.Failure("Owner not found.");
            }

            if (owner.VerificationStatus != VerificationStatus.Pending)
            {
                return ApiResponse.Failure($"Owner application has already been {owner.VerificationStatus.ToString().ToLowerInvariant()}.");
            }

            if (action.Equals("Approve", StringComparison.OrdinalIgnoreCase))
            {
                owner.VerificationStatus = VerificationStatus.Verified;
                owner.BusinessVerifiedAt = DateTime.UtcNow;
                _unitOfWork.ParkingOwners.Update(owner);
                await _unitOfWork.SaveChangesAsync();
                return ApiResponse.Success("Owner application approved successfully.");
            }
            else if (action.Equals("Reject", StringComparison.OrdinalIgnoreCase))
            {
                owner.VerificationStatus = VerificationStatus.Suspended;
                _unitOfWork.ParkingOwners.Update(owner);
                await _unitOfWork.SaveChangesAsync();
                return ApiResponse.Success("Owner application rejected.");
            }

            return ApiResponse.Failure("Action must be 'Approve' or 'Reject'.");
        }

        public async Task<ApiResponse<AdminDriverStatsDTO>> GetDriverStatsAsync()
        {
            var driversQuery = _userManager.Users.Where(u => u.Role == UserRole.Driver);
            var now = DateTimeOffset.UtcNow;

            var totalDrivers = await driversQuery.CountAsync();
            var suspended = await driversQuery.CountAsync(u => u.LockoutEnd != null && u.LockoutEnd > now);
            var pending = await driversQuery.CountAsync(u => (u.LockoutEnd == null || u.LockoutEnd <= now) && !u.EmailConfirmed);
            var active = await driversQuery.CountAsync(u => (u.LockoutEnd == null || u.LockoutEnd <= now) && u.EmailConfirmed);

            var dto = new AdminDriverStatsDTO
            {
                TotalDrivers = totalDrivers,
                Active = active,
                Pending = pending,
                Suspended = suspended
            };

            return ApiResponse<AdminDriverStatsDTO>.Success("Driver statistics retrieved successfully.", dto);
        }

        public async Task<ApiResponse<PagedResult<AdminDriverListItemDTO>>> GetDriversAsync(
            string? status,
            string? search,
            int page,
            int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _userManager.Users.Where(u => u.Role == UserRole.Driver);
            var now = DateTimeOffset.UtcNow;

            var normalizedStatus = status?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(normalizedStatus) && normalizedStatus != "all")
            {
                switch (normalizedStatus)
                {
                    case "suspended":
                        query = query.Where(u => u.LockoutEnd != null && u.LockoutEnd > now);
                        break;
                    case "pending":
                        query = query.Where(u => (u.LockoutEnd == null || u.LockoutEnd <= now) && !u.EmailConfirmed);
                        break;
                    case "active":
                        query = query.Where(u => (u.LockoutEnd == null || u.LockoutEnd <= now) && u.EmailConfirmed);
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(term) ||
                    (u.Email != null && u.Email.ToLower().Contains(term)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.ToLower().Contains(term)));
            }

            var totalItems = await query.CountAsync();

            var rawDrivers = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    u.ProfilePictureUrl,
                    u.CreatedAt,
                    u.EmailConfirmed,
                    u.LockoutEnd,
                    ReservationsCount = u.Reservations.Count(),
                    TotalSpent = u.Reservations
                        .Where(r => r.Status != ReservationStatus.Cancelled)
                        .Sum(r => (decimal?)r.TotalPrice) ?? 0m,
                    LastActive = u.Reservations
                        .OrderByDescending(r => r.ArrivalTime)
                        .Select(r => (DateTime?)r.ArrivalTime)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var dtos = rawDrivers.Select(x =>
            {
                var isSuspended = x.LockoutEnd != null && x.LockoutEnd > now;
                var currentStatus = isSuspended ? "Suspended" : (!x.EmailConfirmed ? "Pending" : "Active");
                var initials = ExtractInitials(x.FullName);

                return new AdminDriverListItemDTO
                {
                    UserId = x.Id,
                    FullName = x.FullName,
                    Initials = initials,
                    Email = x.Email ?? string.Empty,
                    Phone = x.PhoneNumber ?? string.Empty,
                    ProfilePictureUrl = x.ProfilePictureUrl,
                    RegisteredAt = x.CreatedAt,
                    ReservationsCount = x.ReservationsCount,
                    TotalSpent = x.TotalSpent,
                    LastActive = x.LastActive ?? x.CreatedAt,
                    Status = currentStatus
                };
            }).ToList();

            var pagedResult = new PagedResult<AdminDriverListItemDTO>(dtos, totalItems, page, pageSize);
            return ApiResponse<PagedResult<AdminDriverListItemDTO>>.Success("Drivers retrieved successfully.", pagedResult);
        }

        public async Task<ApiResponse<AdminDriverDetailDTO>> GetDriverByIdAsync(Guid userId)
        {
            var user = await _userManager.Users
                .Where(u => u.Id == userId && u.Role == UserRole.Driver)
                .Include(u => u.Reservations)
                    .ThenInclude(r => r.ParkingSpace)
                        .ThenInclude(s => s.Parking)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return ApiResponse<AdminDriverDetailDTO>.Failure("Driver not found.");
            }

            var now = DateTimeOffset.UtcNow;
            var isSuspended = user.LockoutEnd != null && user.LockoutEnd > now;
            var status = isSuspended ? "Suspended" : (!user.EmailConfirmed ? "Pending" : "Active");

            var reservations = user.Reservations ?? new List<Reservation>();
            var nonCancelled = reservations.Where(r => r.Status != ReservationStatus.Cancelled).ToList();
            var totalSpent = nonCancelled.Sum(r => r.TotalPrice);
            var lastActive = reservations.OrderByDescending(r => r.ArrivalTime).Select(r => (DateTime?)r.ArrivalTime).FirstOrDefault() ?? user.CreatedAt;

            var recentReservations = reservations
                .OrderByDescending(r => r.ArrivalTime)
                .Take(10)
                .Select(r => new AdminDriverReservationDTO
                {
                    ReservationId = r.ReservationId,
                    ParkingName = r.ParkingSpace?.Parking?.Name ?? "Unknown Parking",
                    SpotNumber = r.ParkingSpace?.SpotNumber ?? "N/A",
                    ArrivalTime = r.ArrivalTime,
                    DepartureTime = r.DepartureTime,
                    TotalPrice = r.TotalPrice,
                    Status = r.Status.ToString()
                })
                .ToList();

            var dto = new AdminDriverDetailDTO
            {
                UserId = user.Id,
                FullName = user.FullName,
                Initials = ExtractInitials(user.FullName),
                Email = user.Email ?? string.Empty,
                Phone = user.PhoneNumber ?? string.Empty,
                ProfilePictureUrl = user.ProfilePictureUrl,
                CityState = user.CityState,
                Bio = user.Bio,
                RegisteredAt = user.CreatedAt,
                EmailConfirmed = user.EmailConfirmed,
                Status = status,
                ReservationsCount = reservations.Count,
                TotalSpent = totalSpent,
                LastActive = lastActive,
                RecentReservations = recentReservations
            };

            return ApiResponse<AdminDriverDetailDTO>.Success("Driver details retrieved successfully.", dto);
        }

        public async Task<ApiResponse> UpdateDriverStatusAsync(Guid userId, string status)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || user.Role != UserRole.Driver)
            {
                return ApiResponse.Failure("Driver not found.");
            }

            if (status.Equals("Suspended", StringComparison.OrdinalIgnoreCase))
            {
                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));

                var tokens = await _unitOfWork.RefreshTokens.Query()
                    .Where(t => t.UserId == userId && !t.IsRevoked)
                    .ToListAsync();

                foreach (var token in tokens)
                {
                    token.IsRevoked = true;
                    _unitOfWork.RefreshTokens.Update(token);
                }
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse.Success("Driver account suspended successfully.");
            }
            else if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                await _userManager.SetLockoutEndDateAsync(user, null);

                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                }

                return ApiResponse.Success("Driver account activated successfully.");
            }

            return ApiResponse.Failure("Invalid status. Supported values are 'Active' and 'Suspended'.");
        }

        private static string FormatTimeAgo(DateTime timestamp, DateTime now)
        {
            var elapsed = now - timestamp;
            if (elapsed.TotalMinutes < 1) return "just now";
            if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes} min ago";
            if (elapsed.TotalHours < 24)
            {
                var h = (int)elapsed.TotalHours;
                var m = elapsed.Minutes;
                return m > 0 ? $"{h}h {m}m ago" : $"{h}h ago";
            }
            var d = (int)elapsed.TotalDays;
            return d == 1 ? "1 day ago" : $"{d} days ago";
        }

        private static string ExtractInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "?";
            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant()
                : fullName.Length > 0 ? fullName[0].ToString().ToUpperInvariant() : "?";
        }
    }
}