using APIServiceManagement.API.Middleware;
using APIServiceManagement.Application.DTOs.Requests;
using APIServiceManagement.Application.Interfaces.Services;
using APIServiceManagement.Domain.Constants;
using APIServiceManagement.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace APIServiceManagement.API.Configuration;

public static class ApiConfiguration
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        
        // Configure Authorization Policies
        services.AddAuthorization(options =>
        {
            // Admin Policy - allows Admin, MasterAdmin, or DefaultAdmin
            options.AddPolicy("Admin", policy => 
                policy.RequireRole(RoleNames.Admin, RoleNames.MasterAdmin, RoleNames.DefaultAdmin));
            
            // MasterAdmin Policy - allows only MasterAdmin
            options.AddPolicy("MasterAdmin", policy => 
                policy.RequireRole(RoleNames.MasterAdmin));
            
            // Customer Policy - allows only Customer
            options.AddPolicy("Customer", policy => 
                policy.RequireRole(RoleNames.Customer));
            
            // ServiceProvider Policy - allows only ServiceProvider
            options.AddPolicy("ServiceProvider", policy => 
                policy.RequireRole(RoleNames.ServiceProvider));
            
            // ServiceProviderOrCustomer Policy - allows both ServiceProvider and Customer
            options.AddPolicy("ServiceProviderOrCustomer", policy => 
                policy.RequireRole(RoleNames.ServiceProvider, RoleNames.Customer));
        });

        var corsOrigins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
        services.AddCors(options =>
        {
            options.AddPolicy("ApiCorsPolicy", policy =>
            {
                policy.WithOrigins(corsOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        ConfigureJwtAuthentication(services, configuration);

        // Register Infrastructure services
        services.AddInfrastructure(configuration);

        return services;
    }

    public static WebApplication ConfigureApiPipeline(this WebApplication app)
    {
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseCors("ApiCorsPolicy");
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        return app;
    }

    private static void ConfigureJwtAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is not configured."));
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured."),
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured."),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                // Map role claims from token to User.Claims
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = ClaimTypes.NameIdentifier
            };
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = async context =>
                {
                    if (context.Exception is not SecurityTokenExpiredException)
                    {
                        return;
                    }

                    var refreshToken = ResolveRefreshToken(context.HttpContext.Request);
                    if (string.IsNullOrWhiteSpace(refreshToken))
                    {
                        return;
                    }

                    var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                    try
                    {
                        var authResponse = await authService.RefreshTokenAsync(new RefreshTokenRequest
                        {
                            RefreshToken = refreshToken
                        });

                        var handler = new JwtSecurityTokenHandler();
                        var validationParameters = context.Options.TokenValidationParameters.Clone();
                        var principal = handler.ValidateToken(authResponse.Token, validationParameters, out _);

                        context.Principal = principal;
                        context.HttpContext.User = principal;
                        context.Success();

                        if (!context.HttpContext.Response.HasStarted)
                        {
                            var headers = context.HttpContext.Response.Headers;
                            headers["X-Access-Token"] = authResponse.Token;
                            headers["X-Refresh-Token"] = authResponse.RefreshToken;
                            headers["X-Token-Expires-At"] = authResponse.ExpiresAt.ToString("O");
                            headers["X-Refresh-Token-Expires-At"] = authResponse.RefreshTokenExpiresAt.ToString("O");
                        }
                    }
                    catch
                    {
                        // Leave authentication as failed if refresh is invalid/expired.
                    }
                }
            };
        });
    }

    private static string? ResolveRefreshToken(HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-Refresh-Token", out var headerToken))
        {
            return headerToken.ToString().Trim();
        }

        if (request.Headers.TryGetValue("Refresh-Token", out var legacyHeaderToken))
        {
            return legacyHeaderToken.ToString().Trim();
        }

        if (request.Cookies.TryGetValue("refreshToken", out var cookieToken))
        {
            return cookieToken.Trim();
        }

        return null;
    }
}
