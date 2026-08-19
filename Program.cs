
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Options;
using Parkly_Backend.Data;
using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models;
using Parkly_Backend.Models.Enums;
using Parkly_Backend.Services;
using Parkly_Backend.Services.Implemention;
using Parkly_Backend.Services.Interfaces;
using AutoMapper;
using Parkly_Backend.Mappings;
using System;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.OpenApi;

namespace Parkly_Backend
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Env.Load();
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            }).ConfigureApiBehaviorOptions(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });
            var connectionString = Environment.GetEnvironmentVariable("Connection_String");
            // Add services to the container.
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));
            builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(options =>
            {
                // for test and development
                options.Password.RequireDigit = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            }
            ).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = false,
                        ValidateLifetime = false,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = Environment.GetEnvironmentVariable("Issuer"),
                        ValidAudience = Environment.GetEnvironmentVariable("Audience"),
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("SecretKey")))
                    };
                });
            builder.Services.AddScoped<IAccountService, AccountService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IPricingService, PricingService>();
            builder.Services.AddScoped<IReservationsService, ReservationsService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
            // builder.Services.AddScoped<IReservationsService, ReservationsService>();
            // builder.Services.AddScoped<IParkingSpacesService,ParkingSpacesService>();


            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Parkly API",
                    Version = "v1",
                    Description = "REST API for the Parkly parking platform. " +
                        "To call protected endpoints, log in via POST /api/auth/login and use the returned JWT token " +
                        "with the Authorize button (format: Bearer <token>)."
                });

                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT token prefixed with 'Bearer '. Example: Bearer eyJhbGciOi..."
                });

                options.OperationFilter<Parkly_Backend.Swagger.AuthorizeCheckOperationFilter>();
                options.OperationFilter<Parkly_Backend.Swagger.ResponseExamplesOperationFilter>();
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            var enableSwagger = app.Environment.IsDevelopment()
                || (Environment.GetEnvironmentVariable("ENABLE_SWAGGER")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true);
            if (enableSwagger)
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Parkly API v1"));
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();

            app.UseAuthorization();


            app.MapControllers();

            await DataInitialize.InitializeDatabaseAsync(app.Services);

            await app.RunAsync();
        }
    }
}
