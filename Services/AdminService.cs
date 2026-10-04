using System.Text;
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

            return ApiResponse.Failure("Invalid status value. Only 'Active' and 'Suspended' are supported.");
        }

        public async Task<ApiResponse<AdminOwnerStatsDTO>> GetOwnerStatsAsync()
        {
            var query = _unitOfWork.ParkingOwners.Query();
            var totalOwners = await query.CountAsync();
            var active = await query.CountAsync(o => o.VerificationStatus == VerificationStatus.Verified);
            var pending = await query.CountAsync(o => o.VerificationStatus == VerificationStatus.Pending);
            var totalRevenue = await _unitOfWork.Reservations.Query()
                .Where(r => r.Status == ReservationStatus.Completed)
                .SumAsync(r => (decimal?)r.TotalPrice) ?? 0m;

            var dto = new AdminOwnerStatsDTO
            {
                TotalOwners = totalOwners,
                Active = active,
                Pending = pending,
                TotalRevenue = totalRevenue
            };

            return ApiResponse<AdminOwnerStatsDTO>.Success("Parking owner statistics retrieved successfully.", dto);
        }

        public async Task<ApiResponse<PagedResult<AdminOwnerListItemDTO>>> GetOwnersAsync(
            string? status,
            string? search,
            int page,
            int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _unitOfWork.ParkingOwners.Query();

            var normalizedStatus = status?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(normalizedStatus) && normalizedStatus != "all")
            {
                switch (normalizedStatus)
                {
                    case "active":
                        query = query.Where(o => o.VerificationStatus == VerificationStatus.Verified);
                        break;
                    case "pending":
                        query = query.Where(o => o.VerificationStatus == VerificationStatus.Pending);
                        break;
                    case "suspended":
                        query = query.Where(o => o.VerificationStatus == VerificationStatus.Suspended);
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(o =>
                    o.CompanyName.ToLower().Contains(term) ||
                    (o.User != null && o.User.FullName.ToLower().Contains(term)) ||
                    (o.User != null && o.User.Email != null && o.User.Email.ToLower().Contains(term)) ||
                    (o.User != null && o.User.PhoneNumber != null && o.User.PhoneNumber.ToLower().Contains(term)));
            }

            var totalItems = await query.CountAsync();

            var rawOwners = await query
                .Include(o => o.User)
                .OrderByDescending(o => o.User.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    o.OwnerId,
                    OwnerName = o.User != null ? o.User.FullName : "Unknown",
                    Email = o.User != null ? o.User.Email ?? "" : "",
                    Phone = o.User != null ? o.User.PhoneNumber ?? "" : "",
                    RegisteredAt = o.User != null ? o.User.CreatedAt : DateTime.MinValue,
                    o.CompanyName,
                    o.VerificationStatus,
                    LocationsCount = o.Parkings.Count(),
                    TotalSpaces = o.Parkings.SelectMany(p => p.ParkingSpaces).Count(),
                    TotalRevenue = _unitOfWork.Reservations.Query()
                        .Where(r => r.ParkingSpace.Parking.OwnerId == o.OwnerId && r.Status == ReservationStatus.Completed)
                        .Sum(r => (decimal?)r.TotalPrice) ?? 0m
                })
                .ToListAsync();

            var dtos = rawOwners.Select(x =>
            {
                var statusStr = x.VerificationStatus switch
                {
                    VerificationStatus.Verified => "Active",
                    VerificationStatus.Pending => "Pending",
                    VerificationStatus.Suspended => "Suspended",
                    _ => x.VerificationStatus.ToString()
                };

                return new AdminOwnerListItemDTO
                {
                    OwnerId = x.OwnerId,
                    OwnerName = x.OwnerName,
                    Initials = ExtractInitials(x.OwnerName),
                    BusinessName = x.CompanyName,
                    Email = x.Email,
                    Phone = x.Phone,
                    RegisteredAt = x.RegisteredAt,
                    RegisteredDisplay = "Live Registered",
                    LocationsCount = x.LocationsCount,
                    TotalSpaces = x.TotalSpaces,
                    TotalRevenue = x.TotalRevenue,
                    Status = statusStr
                };
            }).ToList();

            var pagedResult = new PagedResult<AdminOwnerListItemDTO>(dtos, totalItems, page, pageSize);
            return ApiResponse<PagedResult<AdminOwnerListItemDTO>>.Success("Parking owners retrieved successfully.", pagedResult);
        }

        public async Task<ApiResponse<AdminOwnerDetailDTO>> GetOwnerByIdAsync(Guid ownerId)
        {
            var owner = await _unitOfWork.ParkingOwners.Query()
                .Include(o => o.User)
                .Include(o => o.Parkings)
                    .ThenInclude(p => p.ParkingSpaces)
                .FirstOrDefaultAsync(o => o.OwnerId == ownerId);

            if (owner == null)
            {
                return ApiResponse<AdminOwnerDetailDTO>.Failure("Parking owner not found.");
            }

            var parkingsList = new List<AdminOwnerParkingDTO>();
            foreach (var p in owner.Parkings)
            {
                var rev = await _unitOfWork.Reservations.Query()
                    .Where(r => r.ParkingSpace.ParkingId == p.ParkingId && r.Status == ReservationStatus.Completed)
                    .SumAsync(r => (decimal?)r.TotalPrice) ?? 0m;

                parkingsList.Add(new AdminOwnerParkingDTO
                {
                    ParkingId = p.ParkingId,
                    Name = p.Name,
                    Address = p.Address,
                    TotalSpaces = p.ParkingSpaces.Count,
                    ActiveSpaces = p.ParkingSpaces.Count(s => s.IsActive),
                    AverageRating = p.AverageRating,
                    TotalRevenue = rev
                });
            }

            var totalRev = parkingsList.Sum(p => p.TotalRevenue);
            var statusStr = owner.VerificationStatus switch
            {
                VerificationStatus.Verified => "Active",
                VerificationStatus.Pending => "Pending",
                VerificationStatus.Suspended => "Suspended",
                _ => owner.VerificationStatus.ToString()
            };

            var dto = new AdminOwnerDetailDTO
            {
                OwnerId = owner.OwnerId,
                OwnerName = owner.User?.FullName ?? "Unknown",
                Initials = ExtractInitials(owner.User?.FullName ?? ""),
                BusinessName = owner.CompanyName,
                Email = owner.User?.Email ?? string.Empty,
                Phone = owner.User?.PhoneNumber ?? string.Empty,
                TaxId = owner.TaxId,
                StreetAddress = owner.StreetAddress,
                CityStateZip = owner.CityStateZip,
                RegisteredAt = owner.User?.CreatedAt ?? DateTime.MinValue,
                BusinessVerifiedAt = owner.BusinessVerifiedAt,
                Status = statusStr,
                LocationsCount = owner.Parkings.Count,
                TotalSpaces = owner.Parkings.Sum(p => p.ParkingSpaces.Count),
                TotalRevenue = totalRev,
                Parkings = parkingsList
            };

            return ApiResponse<AdminOwnerDetailDTO>.Success("Parking owner details retrieved successfully.", dto);
        }

        public async Task<ApiResponse> UpdateOwnerStatusAsync(Guid ownerId, string status)
        {
            var owner = await _unitOfWork.ParkingOwners.GetOwnerWithUserAsync(ownerId);
            if (owner == null)
            {
                return ApiResponse.Failure("Parking owner not found.");
            }

            var user = owner.User ?? await _userManager.FindByIdAsync(ownerId.ToString());

            if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                owner.VerificationStatus = VerificationStatus.Verified;
                owner.BusinessVerifiedAt ??= DateTime.UtcNow;
                _unitOfWork.ParkingOwners.Update(owner);
                await _unitOfWork.SaveChangesAsync();

                if (user != null)
                {
                    await _userManager.SetLockoutEndDateAsync(user, null);
                }

                return ApiResponse.Success("Parking owner status set to Active (Verified).");
            }
            else if (status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                owner.VerificationStatus = VerificationStatus.Pending;
                _unitOfWork.ParkingOwners.Update(owner);
                await _unitOfWork.SaveChangesAsync();

                return ApiResponse.Success("Parking owner status set to Pending.");
            }
            else if (status.Equals("Suspended", StringComparison.OrdinalIgnoreCase))
            {
                owner.VerificationStatus = VerificationStatus.Suspended;
                _unitOfWork.ParkingOwners.Update(owner);
                await _unitOfWork.SaveChangesAsync();

                if (user != null)
                {
                    await _userManager.SetLockoutEnabledAsync(user, true);
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));

                    var tokens = await _unitOfWork.RefreshTokens.Query()
                        .Where(t => t.UserId == ownerId && !t.IsRevoked)
                        .ToListAsync();
                    foreach (var token in tokens)
                    {
                        token.IsRevoked = true;
                        _unitOfWork.RefreshTokens.Update(token);
                    }
                    await _unitOfWork.SaveChangesAsync();
                }

                return ApiResponse.Success("Parking owner suspended successfully.");
            }

            return ApiResponse.Failure("Invalid status. Supported values are 'Active', 'Pending', and 'Suspended'.");
        }

        public async Task<ApiResponse<AdminLocationStatsDTO>> GetLocationStatsAsync()
        {
            var totalLocations = await _unitOfWork.Parkings.Query().CountAsync();
            var active = await _unitOfWork.Parkings.Query()
                .CountAsync(p => p.ParkingSpaces.Any(s => s.IsActive));
            var totalSpaces = await _unitOfWork.ParkingSpaces.Query().CountAsync();
            var networkRevenue = await _unitOfWork.Reservations.Query()
                .Where(r => r.Status == ReservationStatus.Completed)
                .SumAsync(r => (decimal?)r.TotalPrice) ?? 0m;

            var dto = new AdminLocationStatsDTO
            {
                TotalLocations = totalLocations,
                Active = active,
                TotalSpaces = totalSpaces,
                NetworkRevenue = networkRevenue
            };

            return ApiResponse<AdminLocationStatsDTO>.Success("Parking location statistics retrieved successfully.", dto);
        }

        public async Task<ApiResponse<PagedResult<AdminLocationListItemDTO>>> GetLocationsAsync(
            string? status,
            string? search,
            int page,
            int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _unitOfWork.Parkings.Query();

            var normalizedStatus = status?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(normalizedStatus) && normalizedStatus != "all")
            {
                if (normalizedStatus == "active")
                {
                    query = query.Where(p => p.ParkingSpaces.Any(s => s.IsActive));
                }
                else if (normalizedStatus == "inactive")
                {
                    query = query.Where(p => !p.ParkingSpaces.Any(s => s.IsActive));
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    p.Address.ToLower().Contains(term) ||
                    (p.ParkingOwner != null && (
                        p.ParkingOwner.CompanyName.ToLower().Contains(term) ||
                        (p.ParkingOwner.User != null && p.ParkingOwner.User.FullName.ToLower().Contains(term))
                    )));
            }

            var totalItems = await query.CountAsync();

            var rawLocations = await query
                .Include(p => p.ParkingOwner)
                    .ThenInclude(o => o.User)
                .Include(p => p.ParkingSpaces)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.ParkingId,
                    LocationName = p.Name,
                    p.OwnerId,
                    OwnerName = !string.IsNullOrEmpty(p.ParkingOwner.CompanyName)
                        ? p.ParkingOwner.CompanyName
                        : (p.ParkingOwner.User != null ? p.ParkingOwner.User.FullName : "Unknown"),
                    p.Address,
                    p.AverageRating,
                    p.CreatedAt,
                    TotalSpaces = p.ParkingSpaces.Count,
                    ActiveSpaces = p.ParkingSpaces.Count(s => s.IsActive),
                    TotalRevenue = _unitOfWork.Reservations.Query()
                        .Where(r => r.ParkingSpace.ParkingId == p.ParkingId && r.Status == ReservationStatus.Completed)
                        .Sum(r => (decimal?)r.TotalPrice) ?? 0m
                })
                .ToListAsync();

            var dtos = rawLocations.Select(x =>
            {
                var isActive = x.ActiveSpaces > 0;
                return new AdminLocationListItemDTO
                {
                    ParkingId = x.ParkingId,
                    LocationName = x.LocationName,
                    OwnerId = x.OwnerId,
                    OwnerName = x.OwnerName,
                    City = ExtractCity(x.Address),
                    Address = x.Address,
                    TotalSpaces = x.TotalSpaces,
                    ActiveSpaces = x.ActiveSpaces,
                    TotalRevenue = x.TotalRevenue,
                    AverageRating = x.AverageRating,
                    Status = isActive ? "Active" : "Inactive",
                    IsActive = isActive,
                    CreatedAt = x.CreatedAt
                };
            }).ToList();

            var pagedResult = new PagedResult<AdminLocationListItemDTO>(dtos, totalItems, page, pageSize);
            return ApiResponse<PagedResult<AdminLocationListItemDTO>>.Success("Parking locations retrieved successfully.", pagedResult);
        }

        public async Task<ApiResponse<AdminLocationDetailDTO>> GetLocationByIdAsync(Guid parkingId)
        {
            var parking = await _unitOfWork.Parkings.Query()
                .Include(p => p.ParkingOwner)
                    .ThenInclude(o => o.User)
                .Include(p => p.ParkingSpaces)
                .Include(p => p.PricingRules)
                .FirstOrDefaultAsync(p => p.ParkingId == parkingId);

            if (parking == null)
            {
                return ApiResponse<AdminLocationDetailDTO>.Failure("Parking location not found.");
            }

            var totalRevenue = await _unitOfWork.Reservations.Query()
                .Where(r => r.ParkingSpace.ParkingId == parkingId && r.Status == ReservationStatus.Completed)
                .SumAsync(r => (decimal?)r.TotalPrice) ?? 0m;

            var occupied = await _unitOfWork.Reservations.GetCheckedInCountForParkingAsync(parkingId);
            var activeSpaces = parking.ParkingSpaces.Count(s => s.IsActive);
            var availableSpaces = Math.Max(0, activeSpaces - occupied);
            var isActive = activeSpaces > 0;

            var dto = new AdminLocationDetailDTO
            {
                ParkingId = parking.ParkingId,
                LocationName = parking.Name,
                OwnerId = parking.OwnerId,
                OwnerName = parking.ParkingOwner?.User?.FullName ?? "Unknown",
                OwnerBusinessName = parking.ParkingOwner?.CompanyName ?? string.Empty,
                OwnerEmail = parking.ParkingOwner?.User?.Email ?? string.Empty,
                OwnerPhone = parking.ParkingOwner?.User?.PhoneNumber ?? string.Empty,
                City = ExtractCity(parking.Address),
                Address = parking.Address,
                Latitude = parking.Latitude,
                Longitude = parking.Longitude,
                OperatingHours = parking.OperatingHours,
                TotalSpaces = parking.ParkingSpaces.Count,
                ActiveSpaces = activeSpaces,
                OccupiedSpaces = occupied,
                AvailableSpaces = availableSpaces,
                TotalRevenue = totalRevenue,
                AverageRating = parking.AverageRating,
                TotalReviews = parking.TotalReviews,
                Status = isActive ? "Active" : "Inactive",
                IsActive = isActive,
                CreatedAt = parking.CreatedAt,
                Features = parking.Features.Select(f => f.ToString()).ToList(),
                PricingRules = parking.PricingRules.Select(pr => new AdminLocationPricingRuleDTO
                {
                    RuleId = pr.RuleId,
                    RuleType = pr.RuleType.ToString(),
                    StartTime = pr.StartTime,
                    EndTime = pr.EndTime,
                    PriceModifier = pr.PriceModifier
                }).ToList()
            };

            return ApiResponse<AdminLocationDetailDTO>.Success("Parking location details retrieved successfully.", dto);
        }

        public async Task<ApiResponse> UpdateLocationStatusAsync(Guid parkingId, string status)
        {
            var parking = await _unitOfWork.Parkings.Query()
                .Include(p => p.ParkingSpaces)
                .FirstOrDefaultAsync(p => p.ParkingId == parkingId);

            if (parking == null)
            {
                return ApiResponse.Failure("Parking location not found.");
            }

            var makeActive = status.Equals("Active", StringComparison.OrdinalIgnoreCase);
            var makeInactive = status.Equals("Inactive", StringComparison.OrdinalIgnoreCase);

            if (!makeActive && !makeInactive)
            {
                return ApiResponse.Failure("Invalid status. Supported values are 'Active' and 'Inactive'.");
            }

            foreach (var space in parking.ParkingSpaces)
            {
                space.IsActive = makeActive;
                _unitOfWork.ParkingSpaces.Update(space);
            }

            await _unitOfWork.SaveChangesAsync();

            var message = makeActive
                ? "Parking location activated successfully."
                : "Parking location deactivated successfully.";

            return ApiResponse.Success(message);
        }

        public async Task<ApiResponse<AdminReservationStatsDTO>> GetReservationStatsAsync()
        {
            var query = _unitOfWork.Reservations.Query();

            var total = await query.CountAsync();
            var activeNow = await query.CountAsync(r => r.Status == ReservationStatus.CheckedIn);
            var upcoming = await query.CountAsync(r => r.Status == ReservationStatus.Confirmed);
            var completed = await query.CountAsync(r => r.Status == ReservationStatus.Completed);
            var cancelled = await query.CountAsync(r => r.Status == ReservationStatus.Cancelled);
            var revenue = await query
                .Where(r => r.Status == ReservationStatus.Completed)
                .SumAsync(r => (decimal?)r.TotalPrice) ?? 0m;

            var dto = new AdminReservationStatsDTO
            {
                TotalReservations = total,
                ActiveNow = activeNow,
                Upcoming = upcoming,
                RevenueProcessed = revenue,
                AllCount = total,
                UpcomingCount = upcoming,
                ActiveCount = activeNow,
                CompletedCount = completed,
                CancelledCount = cancelled
            };

            return ApiResponse<AdminReservationStatsDTO>.Success("Reservation statistics retrieved successfully.", dto);
        }

        public async Task<ApiResponse<PagedResult<AdminReservationListItemDTO>>> GetReservationsAsync(
            string? status,
            string? search,
            int page,
            int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _unitOfWork.Reservations.Query();

            var normalizedStatus = status?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(normalizedStatus) && normalizedStatus != "all")
            {
                query = normalizedStatus switch
                {
                    "upcoming" => query.Where(r => r.Status == ReservationStatus.Confirmed),
                    "active" or "active-now" => query.Where(r => r.Status == ReservationStatus.CheckedIn),
                    "completed" => query.Where(r => r.Status == ReservationStatus.Completed),
                    "cancelled" or "canceled" => query.Where(r => r.Status == ReservationStatus.Cancelled),
                    _ => query
                };
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                var cleanRef = term.Replace("pk-", "");
                query = query.Where(r =>
                    (r.User != null && r.User.FullName.ToLower().Contains(term)) ||
                    (r.User != null && r.User.Email != null && r.User.Email.ToLower().Contains(term)) ||
                    (r.ParkingSpace.Parking != null && r.ParkingSpace.Parking.Name.ToLower().Contains(term)) ||
                    r.ParkingSpace.SpotNumber.ToLower().Contains(term) ||
                    r.ReservationId.ToString().ToLower().Contains(cleanRef));
            }

            var totalItems = await query.CountAsync();

            var rawReservations = await query
                .Include(r => r.User)
                .Include(r => r.ParkingSpace)
                    .ThenInclude(s => s.Parking)
                .OrderBy(r => r.Status == ReservationStatus.CheckedIn ? 0 :
                    r.Status == ReservationStatus.Confirmed ? 1 :
                    r.Status == ReservationStatus.Completed ? 2 : 3)
                .ThenByDescending(r => r.ArrivalTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    r.ReservationId,
                    CustomerName = r.User != null ? r.User.FullName : "Unknown",
                    CustomerEmail = r.User != null ? r.User.Email ?? "" : "",
                    LocationName = r.ParkingSpace.Parking != null ? r.ParkingSpace.Parking.Name : "Unknown",
                    ParkingId = r.ParkingSpace.ParkingId,
                    SpaceNumber = r.ParkingSpace.SpotNumber,
                    SpaceId = r.SpaceId,
                    r.ArrivalTime,
                    r.DepartureTime,
                    r.TotalPrice,
                    r.Status
                })
                .ToListAsync();

            var dtos = rawReservations.Select(x =>
            {
                var duration = x.DepartureTime - x.ArrivalTime;
                return new AdminReservationListItemDTO
                {
                    ReservationId = x.ReservationId,
                    Code = $"PK-{x.ReservationId.ToString("N")[^4..].ToUpperInvariant()}",
                    CustomerId = Guid.Empty,
                    CustomerName = x.CustomerName,
                    CustomerInitials = ExtractInitials(x.CustomerName),
                    CustomerEmail = x.CustomerEmail,
                    Location = x.LocationName,
                    ParkingId = x.ParkingId,
                    Space = x.SpaceNumber,
                    SpaceId = x.SpaceId,
                    Date = x.ArrivalTime.ToString("MMM dd, yyyy"),
                    Time = $"{x.ArrivalTime:hh:mm tt} - {x.DepartureTime:hh:mm tt}",
                    Duration = FormatDuration(duration),
                    Amount = x.TotalPrice,
                    Status = ToReservationStatusDisplay(x.Status),
                    ArrivalTime = x.ArrivalTime,
                    DepartureTime = x.DepartureTime
                };
            }).ToList();

            var pagedResult = new PagedResult<AdminReservationListItemDTO>(dtos, totalItems, page, pageSize);
            return ApiResponse<PagedResult<AdminReservationListItemDTO>>.Success("Reservations retrieved successfully.", pagedResult);
        }

        public async Task<ApiResponse<AdminReservationDetailDTO>> GetReservationByIdAsync(Guid reservationId)
        {
            var reservation = await _unitOfWork.Reservations.Query()
                .Include(r => r.User)
                .Include(r => r.Review)
                .Include(r => r.ParkingSpace)
                    .ThenInclude(s => s.Parking)
                        .ThenInclude(p => p.ParkingOwner)
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

            if (reservation == null)
            {
                return ApiResponse<AdminReservationDetailDTO>.Failure("Reservation not found.");
            }

            var duration = reservation.DepartureTime - reservation.ArrivalTime;
            var customerName = reservation.User != null ? reservation.User.FullName : "Unknown";

            var dto = new AdminReservationDetailDTO
            {
                ReservationId = reservation.ReservationId,
                Code = $"PK-{reservation.ReservationId.ToString("N")[^4..].ToUpperInvariant()}",
                Status = ToReservationStatusDisplay(reservation.Status),
                TotalPrice = reservation.TotalPrice,
                QrCode = reservation.QrCode,
                ArrivalTime = reservation.ArrivalTime,
                DepartureTime = reservation.DepartureTime,
                DateFormatted = reservation.ArrivalTime.ToString("MMM dd, yyyy"),
                TimeWindowFormatted = $"{reservation.ArrivalTime:hh:mm tt} - {reservation.DepartureTime:hh:mm tt}",
                DurationFormatted = FormatDuration(duration),
                CustomerId = reservation.UserId,
                CustomerName = customerName,
                CustomerInitials = ExtractInitials(customerName),
                CustomerEmail = reservation.User?.Email ?? string.Empty,
                CustomerPhone = reservation.User?.PhoneNumber ?? string.Empty,
                ParkingId = reservation.ParkingSpace?.ParkingId ?? Guid.Empty,
                ParkingName = reservation.ParkingSpace?.Parking?.Name ?? "Unknown Parking",
                ParkingAddress = reservation.ParkingSpace?.Parking?.Address ?? string.Empty,
                OwnerBusinessName = reservation.ParkingSpace?.Parking?.ParkingOwner?.CompanyName ?? string.Empty,
                SpaceId = reservation.SpaceId,
                SpotNumber = reservation.ParkingSpace?.SpotNumber ?? "N/A",
                Level = reservation.ParkingSpace?.Level,
                SpaceType = reservation.ParkingSpace?.SpaceType.ToString() ?? "Standard",
                VehicleSize = reservation.ParkingSpace?.VehicleSize?.ToString(),
                ReviewRating = reservation.Review?.Rating,
                ReviewComment = reservation.Review?.Comment,
                ReviewDate = reservation.Review?.CreatedAt
            };

            return ApiResponse<AdminReservationDetailDTO>.Success("Reservation details retrieved successfully.", dto);
        }

        public async Task<ApiResponse> CancelReservationAsync(Guid reservationId, string? reason)
        {
            var reservation = await _unitOfWork.Reservations.Query()
                .Include(r => r.ParkingSpace)
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

            if (reservation == null)
            {
                return ApiResponse.Failure("Reservation not found.");
            }

            if (reservation.Status == ReservationStatus.Cancelled)
            {
                return ApiResponse.Failure("Reservation is already cancelled.");
            }

            if (reservation.Status == ReservationStatus.Completed)
            {
                return ApiResponse.Failure("Cannot cancel a completed reservation.");
            }

            reservation.Status = ReservationStatus.Cancelled;
            if (reservation.ParkingSpace != null)
            {
                reservation.ParkingSpace.Status = SpaceStatus.Available;
                _unitOfWork.ParkingSpaces.Update(reservation.ParkingSpace);
            }

            _unitOfWork.Reservations.Update(reservation);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse.Success("Reservation cancelled successfully.");
        }

        private static string ToReservationStatusDisplay(ReservationStatus status) => status switch
        {
            ReservationStatus.Confirmed => "Upcoming",
            ReservationStatus.CheckedIn => "Active",
            ReservationStatus.Completed => "Completed",
            ReservationStatus.Cancelled => "Cancelled",
            _ => status.ToString()
        };

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration < TimeSpan.Zero) duration = TimeSpan.Zero;
            var totalHours = duration.TotalHours;
            if (totalHours < 1)
            {
                return $"{Math.Max(1, (int)Math.Ceiling(duration.TotalMinutes))}m";
            }
            return Math.Abs(totalHours - Math.Round(totalHours)) < 0.01
                ? $"{(int)Math.Round(totalHours)}h"
                : $"{Math.Round(totalHours, 1)}h";
        }

        public async Task<ApiResponse<AdminAnalyticsDTO>> GetAnalyticsAsync(string? period)
        {
            var normalizedPeriod = period?.Trim().ToLowerInvariant() switch
            {
                "this_week" or "week" or "weekly" => "this_week",
                "this_month" or "month" or "monthly" => "this_month",
                _ => "last_6_months"
            };

            var now = DateTime.UtcNow;
            var windowStart = normalizedPeriod switch
            {
                "this_week" => now.Date.AddDays(-7),
                "this_month" => now.Date.AddDays(-30),
                _ => now.Date.AddMonths(-6)
            };

            var parkings = await _unitOfWork.Parkings.Query()
                .Include(p => p.ParkingSpaces)
                .ToListAsync();
            var activeLocations = parkings.Count(p => p.ParkingSpaces.Any(s => s.IsActive));
            var activeSpacesCount = parkings.SelectMany(p => p.ParkingSpaces).Count(s => s.IsActive);

            var reservations = await _unitOfWork.Reservations.Query()
                .Include(r => r.ParkingSpace)
                    .ThenInclude(s => s.Parking)
                .Where(r => r.ArrivalTime >= windowStart)
                .ToListAsync();

            var totalRevenue = reservations
                .Where(r => r.Status == ReservationStatus.Completed)
                .Sum(r => r.TotalPrice);
            var totalBookings = reservations.Count;

            var checkedIn = await _unitOfWork.Reservations.Query()
                .CountAsync(r => r.Status == ReservationStatus.CheckedIn);
            var avgOccupancy = activeSpacesCount > 0
                ? Math.Round(Math.Min(100.0, (double)checkedIn * 100.0 / activeSpacesCount), 1)
                : 0.0;

            List<AdminTrendDataPointDTO> trendPoints;
            if (normalizedPeriod == "this_week")
            {
                trendPoints = Enumerable.Range(0, 7)
                    .Select(i =>
                    {
                        var day = now.Date.AddDays(-(6 - i));
                        var nextDay = day.AddDays(1);
                        return new AdminTrendDataPointDTO
                        {
                            Label = day.ToString("ddd"),
                            Date = day,
                            Bookings = reservations.Count(r => r.ArrivalTime >= day && r.ArrivalTime < nextDay),
                            Revenue = reservations
                                .Where(r => r.Status == ReservationStatus.Completed && r.ArrivalTime >= day && r.ArrivalTime < nextDay)
                                .Sum(r => r.TotalPrice)
                        };
                    })
                    .ToList();
            }
            else if (normalizedPeriod == "this_month")
            {
                trendPoints = Enumerable.Range(0, 4)
                    .Select(weeksAgo =>
                    {
                        var weekEnd = now.Date.AddDays(-(weeksAgo * 7));
                        var weekStart = weekEnd.AddDays(-6);
                        return new AdminTrendDataPointDTO
                        {
                            Label = $"Week {4 - weeksAgo}",
                            Date = weekStart,
                            Bookings = reservations.Count(r => r.ArrivalTime.Date >= weekStart && r.ArrivalTime.Date <= weekEnd),
                            Revenue = reservations
                                .Where(r => r.Status == ReservationStatus.Completed && r.ArrivalTime.Date >= weekStart && r.ArrivalTime.Date <= weekEnd)
                                .Sum(r => r.TotalPrice)
                        };
                    })
                    .Reverse()
                    .ToList();
            }
            else
            {
                trendPoints = Enumerable.Range(0, 12)
                    .Select(monthsAgo =>
                    {
                        var month = now.AddMonths(-monthsAgo);
                        var monthStart = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                        var monthEnd = monthStart.AddMonths(1);
                        return new AdminTrendDataPointDTO
                        {
                            Label = month.ToString("MMM"),
                            Date = monthStart,
                            Bookings = reservations.Count(r => r.ArrivalTime >= monthStart && r.ArrivalTime < monthEnd),
                            Revenue = reservations
                                .Where(r => r.Status == ReservationStatus.Completed && r.ArrivalTime >= monthStart && r.ArrivalTime < monthEnd)
                                .Sum(r => r.TotalPrice)
                        };
                    })
                    .Reverse()
                    .ToList();
            }

            var owners = await _unitOfWork.ParkingOwners.Query()
                .Include(o => o.User)
                .Include(o => o.Parkings)
                .ToListAsync();

            var revenueByOwner = owners.Select(o =>
            {
                var displayName = !string.IsNullOrWhiteSpace(o.CompanyName)
                    ? o.CompanyName
                    : (o.User != null && !string.IsNullOrWhiteSpace(o.User.FullName)
                        ? o.User.FullName
                        : $"Owner {o.OwnerId.ToString("N")[..8]}");

                var ownerReservations = reservations
                    .Where(r => r.ParkingSpace?.Parking?.OwnerId == o.OwnerId)
                    .ToList();

                var ownerRevenue = ownerReservations
                    .Where(r => r.Status == ReservationStatus.Completed)
                    .Sum(r => r.TotalPrice);

                var share = totalRevenue > 0
                    ? Math.Round((double)(ownerRevenue / totalRevenue) * 100.0, 1)
                    : 0.0;

                return new AdminOwnerRevenueShareDTO
                {
                    OwnerId = o.OwnerId,
                    OwnerName = displayName,
                    LocationsCount = o.Parkings.Count,
                    Revenue = ownerRevenue,
                    SharePercentage = share
                };
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

            var bookingsByOwner = owners.Select(o =>
            {
                var displayName = !string.IsNullOrWhiteSpace(o.CompanyName)
                    ? o.CompanyName
                    : (o.User != null && !string.IsNullOrWhiteSpace(o.User.FullName)
                        ? o.User.FullName
                        : $"Owner {o.OwnerId.ToString("N")[..8]}");

                var ownerBookings = reservations
                    .Count(r => r.ParkingSpace?.Parking?.OwnerId == o.OwnerId);

                var share = totalBookings > 0
                    ? Math.Round((double)ownerBookings * 100.0 / totalBookings, 1)
                    : 0.0;

                return new AdminOwnerBookingsShareDTO
                {
                    OwnerId = o.OwnerId,
                    OwnerName = displayName,
                    BookingsCount = ownerBookings,
                    SharePercentage = share
                };
            })
            .OrderByDescending(x => x.BookingsCount)
            .ToList();

            var dto = new AdminAnalyticsDTO
            {
                Period = normalizedPeriod,
                Summary = new AdminAnalyticsSummaryDTO
                {
                    TotalRevenue = totalRevenue,
                    TotalBookings = totalBookings,
                    AvgOccupancy = avgOccupancy,
                    ActiveLocations = activeLocations
                },
                Trend = trendPoints,
                RevenueByOwner = revenueByOwner,
                BookingsByOwner = bookingsByOwner
            };

            return ApiResponse<AdminAnalyticsDTO>.Success("Analytics data retrieved successfully.", dto);
        }

        public async Task<ApiResponse<string>> ExportAnalyticsCsvAsync(string? period)
        {
            var analyticsRes = await GetAnalyticsAsync(period);
            if (!analyticsRes.IsSuccess || analyticsRes.Data == null)
            {
                return ApiResponse<string>.Failure("Failed to generate analytics data for export.");
            }

            var data = analyticsRes.Data;
            var sb = new StringBuilder();
            sb.AppendLine($"# Parkly Platform Analytics Report ({data.Period})");
            sb.AppendLine($"# Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"# Total Revenue: ${data.Summary.TotalRevenue:F2}, Total Bookings: {data.Summary.TotalBookings}, Avg Occupancy: {data.Summary.AvgOccupancy}%, Active Locations: {data.Summary.ActiveLocations}");
            sb.AppendLine();
            sb.AppendLine("Owner,Locations,Revenue,Revenue Share %,Bookings,Bookings Share %");

            foreach (var rev in data.RevenueByOwner)
            {
                var b = data.BookingsByOwner.FirstOrDefault(x => x.OwnerId == rev.OwnerId);
                var bookings = b?.BookingsCount ?? 0;
                var bShare = b?.SharePercentage ?? 0.0;
                var safeName = rev.OwnerName.Contains(',') ? $"\"{rev.OwnerName}\"" : rev.OwnerName;
                sb.AppendLine($"{safeName},{rev.LocationsCount},{rev.Revenue:F2},{rev.SharePercentage}%,{bookings},{bShare}%");
            }

            return ApiResponse<string>.Success("Analytics CSV exported successfully.", sb.ToString());
        }

        private static string ExtractCity(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return string.Empty;
            var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 ? parts[1] : parts[0];
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