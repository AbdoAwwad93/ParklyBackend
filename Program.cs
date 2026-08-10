
using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parkly_Backend.Data;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models;
using Parkly_Backend.Models.Enums;
using Parkly_Backend.Services;
using Parkly_Backend.Services.Implemention;
using Parkly_Backend.Services.Interfaces;
using System;

namespace Parkly_Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Env.Load();
            //var connectionString = Environment.GetEnvironmentVariable("Connection_String");
            var connectionString = builder.Configuration.GetConnectionString("Connection_String");
            // Add services to the container.
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));
            builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            }

            ).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
            builder.Services.AddScoped<IAccountService,AccountService>();
           // builder.Services.AddScoped<IReservationsService, ReservationsService>();
           // builder.Services.AddScoped<IParkingSpacesService,ParkingSpacesService>();

            

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwaggerUI(options =>
                options.SwaggerEndpoint("/openApi/v1.json","v1"));
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
