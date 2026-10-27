using System.Text;
using System.Text.Json.Serialization;
using HrEmployeeManagementSystem.Api.Data;
using HrEmployeeManagementSystem.Api.Models;
using HrEmployeeManagementSystem.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace HrEmployeeManagementSystem.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var testing = builder.Environment.IsEnvironment("Testing");
        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            if (testing)
                options.UseInMemoryDatabase("HrEmployeeManagementSystemTests");
            else
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
        });

        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        var jwt = builder.Configuration.GetSection("Jwt");
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidAudience = jwt["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                        jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.")))
                };
            });

        builder.Services.AddAuthorization();
        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var message = string.Join(" ", context.ModelState.Values
                    .SelectMany(value => value.Errors)
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Invalid value." : error.ErrorMessage));
                return new BadRequestObjectResult(new { message });
            };
        });

        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"];
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("frontend", policy =>
                policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod());
        });

        builder.Services.AddScoped<TokenService>();
        builder.Services.AddScoped<DashboardService>();
        builder.Services.AddScoped<ReportService>();
        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "CoreHR Employee Management API";
                document.Info.Version = "v1";
                document.Info.Description = "People, attendance, leave, recruitment, and reports for HrEmployeeManagementSystem.";
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Paste the JWT returned by POST /api/auth/login."
                };
                document.Security ??= [];
                document.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
                return Task.CompletedTask;
            });
        });

        var app = builder.Build();
        await DatabaseStartup.InitializeAsync(app);

        if (!app.Environment.IsEnvironment("Testing"))
        {
            app.MapOpenApi();
            app.MapGet("/swagger", () => Results.Content(
                """
                <!doctype html>
                <html lang="en">
                <head>
                  <meta charset="utf-8" />
                  <title>CoreHR API</title>
                  <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css" />
                </head>
                <body>
                  <div id="swagger"></div>
                  <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
                  <script>
                    SwaggerUIBundle({
                      url: "/openapi/v1.json",
                      dom_id: "#swagger",
                      persistAuthorization: true
                    });
                  </script>
                </body>
                </html>
                """,
                "text/html"));
        }

        app.UseCors("frontend");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}

public static class DatabaseStartup
{
    public static async Task InitializeAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (app.Environment.IsEnvironment("Testing"))
        {
            await db.Database.EnsureCreatedAsync();
        }
        else
        {
            var delays = new[] { 1, 2, 4, 8, 10 };
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    await db.Database.MigrateAsync();
                    break;
                }
                catch (Exception ex) when (attempt < delays.Length)
                {
                    app.Logger.LogWarning(ex, "SQL Server is not ready (attempt {Attempt}). Retrying.", attempt + 1);
                    await Task.Delay(TimeSpan.FromSeconds(delays[attempt]));
                }
            }
        }

        await DbSeeder.SeedAsync(scope.ServiceProvider);
    }
}
