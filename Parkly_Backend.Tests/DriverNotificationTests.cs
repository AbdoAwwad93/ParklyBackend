using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Parkly_Backend.Controllers;
using Parkly_Backend.Data;
using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Interfaces.Repositories;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Enums;
using Parkly_Backend.Services;
using Xunit;

namespace Parkly_Backend.Tests
{
    public class DriverNotificationTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task NotificationService_GetForUserAsync_ReturnsDriverNotifications()
        {
            using var context = GetInMemoryDbContext();
            var unitOfWork = new UnitOfWork(context);
            var notificationService = new NotificationService(unitOfWork);

            var driverId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            await notificationService.CreateAsync(driverId, NotificationType.Booking, "Booking Confirmed", "Your reservation is confirmed.");
            await notificationService.CreateAsync(driverId, NotificationType.Update, "Check-in Confirmed", "You have checked in.");
            await notificationService.CreateAsync(otherUserId, NotificationType.Booking, "Other User Booking", "Other reservation.");

            var result = await notificationService.GetForUserAsync(driverId, null, null, 1, 10);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.TotalCount);
            Assert.All(result.Data.Items, item => Assert.True(item.Title == "Booking Confirmed" || item.Title == "Check-in Confirmed"));
        }

        [Fact]
        public async Task NotificationService_GetSummaryAndMarkRead_WorksForDriver()
        {
            using var context = GetInMemoryDbContext();
            var unitOfWork = new UnitOfWork(context);
            var notificationService = new NotificationService(unitOfWork);

            var driverId = Guid.NewGuid();

            await notificationService.CreateAsync(driverId, NotificationType.Booking, "Booking 1", "Message 1");
            await notificationService.CreateAsync(driverId, NotificationType.Booking, "Booking 2", "Message 2");

            var summary = await notificationService.GetSummaryAsync(driverId);
            Assert.True(summary.IsSuccess);
            Assert.Equal(2, summary.Data!.UnreadCount);

            var driverPage = await notificationService.GetForUserAsync(driverId, null, false, 1, 10);
            var firstNotificationId = driverPage.Data!.Items[0].Id;

            var markReadResult = await notificationService.MarkReadAsync(driverId, firstNotificationId);
            Assert.True(markReadResult.IsSuccess);

            var summaryAfterMarkRead = await notificationService.GetSummaryAsync(driverId);
            Assert.Equal(1, summaryAfterMarkRead.Data!.UnreadCount);
        }

        [Fact]
        public async Task NotificationService_MarkAllReadAsync_CallsRepository()
        {
            var driverId = Guid.NewGuid();
            var mockUow = new Mock<IUnitOfWork>();
            var mockRepo = new Mock<INotificationsRepository>();

            mockRepo.Setup(r => r.MarkAllReadAsync(driverId, It.IsAny<DateTime>()))
                .ReturnsAsync(2);

            mockUow.Setup(u => u.Notifications).Returns(mockRepo.Object);

            var service = new NotificationService(mockUow.Object);
            var result = await service.MarkAllReadAsync(driverId);

            Assert.True(result.IsSuccess);
            mockRepo.Verify(r => r.MarkAllReadAsync(driverId, It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task NotificationsController_GetAll_ReturnsOkWithDriverData()
        {
            var driverId = Guid.NewGuid();
            var mockService = new Mock<INotificationService>();

            mockService.Setup(s => s.GetForUserAsync(driverId, null, null, 1, 20))
                .ReturnsAsync(Parkly_Backend.Models.Response.ApiResponse<NotificationPageDTO>.Success("Success", new NotificationPageDTO
                {
                    Items = new List<NotificationDTO>
                    {
                        new NotificationDTO { Id = Guid.NewGuid(), Title = "Booking Confirmed", Message = "Driver notification" }
                    },
                    Page = 1,
                    PageSize = 20,
                    TotalCount = 1,
                    TotalPages = 1
                }));

            var controller = new NotificationsController(mockService.Object);
            var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, driverId.ToString()),
                new Claim(ClaimTypes.Role, "Driver")
            }, "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userPrincipal }
            };

            var actionResult = await controller.GetAll(null, null, 1, 20);
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var response = Assert.IsType<Parkly_Backend.Models.Response.ApiResponse<NotificationPageDTO>>(okResult.Value);

            Assert.True(response.IsSuccess);
            Assert.Equal(1, response.Data!.TotalCount);
            Assert.Equal("Booking Confirmed", response.Data.Items[0].Title);
        }
    }
}
