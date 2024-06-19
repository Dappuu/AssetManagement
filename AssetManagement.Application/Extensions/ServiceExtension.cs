﻿using AssetManagement.Application.ConfigurationOptions;
using AssetManagement.Application.Services.Implementations;
using AssetManagement.Application.Services.Interfaces;
using AssetManagement.Contracts.Dtos.PaginationDtos;
using AssetManagement.Data.Data;
using AssetManagement.Data.Interfaces.Base;
using AssetManagement.Data.Repositories.Base;
using AssetManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System;
using System.Reflection;
using System.Text.Json;

namespace AssetManagement.Application.Extensions;

public static class ServiceExtension
{
    public static void ConfigureDatabase(this IServiceCollection services, AppSetting appsetting)
    {
        services.AddDbContextPool<AssetManagementDbContext>(options => { options.UseSqlServer(appsetting.ConnectionStrings.DefaultConnection); });

        //add identity
        services.AddIdentity<AppUser, Role>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;

            options.User.RequireUniqueEmail = true;

            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 3;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2);
        })
            .AddSignInManager<SignInManager<AppUser>>()
            .AddEntityFrameworkStores<AssetManagementDbContext>()
            .AddDefaultTokenProviders();

    }

    public static void ConfigureCors(this IServiceCollection services) =>
             services.AddCors(options =>
             {
                 options.AddPolicy("CorsPolicy", builder =>
                 builder.AllowAnyOrigin()
                 .AllowAnyMethod()
                 .AllowAnyHeader());
             });

    public static void ConfigureAutoMapper(this IServiceCollection services) => services.AddAutoMapper(typeof(MappingProfile).Assembly);

    public static void ConfigureSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            var startTime = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).ToString();

            var swaggerDescription = "## Description. \n\n" +
            "- This is a list of AssetManagement APIs\n\n" +
            "\n\n" +
            $"* Last updated at: __{startTime}__ \n\n";

            OpenApiInfo apiInfo = new OpenApiInfo
            {
                Title = "AssetManagement Swagger",
                Description = swaggerDescription,
                Version = "v1"
            };
            options.SwaggerDoc("v1", apiInfo);

            // Generating api description via xml;
            string xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

            // Add authentication button
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Enter your token here. The token is in JWT format",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type=ReferenceType.SecurityScheme,
                                Id="Bearer"
                            },
                        },
                        Array.Empty<string>()
                    }
                });
        });
    }

    public static void AddPaginationHeader(this HttpResponse response, PaginationMetaData pagination)
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        response.Headers.Add("Access-Control-Allow-Origin", "*");

        response.Headers.Add("X-Pagination", JsonSerializer.Serialize(pagination, options));

        response.Headers.Add("Access-Control-Expose-Headers", "X-Pagination");
    }

    public static void RegisterServiceDependencies(this IServiceCollection services)
    {
        //Add service DI
        services.AddScoped<IUserService, UserService>();
    }

    public static void RegisterRepositoryDependencies(this IServiceCollection services)
    {
        //Add repository DI
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        //services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

}
