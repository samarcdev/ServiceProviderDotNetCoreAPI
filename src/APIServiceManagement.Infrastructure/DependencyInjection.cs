using APIServiceManagement.Application.Interfaces.Services;
using APIServiceManagement.Infrastructure.Data;
using APIServiceManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace APIServiceManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register DbContext with timeout settings
        services.AddDbContext<AppDbContext>(options =>
        {
            // Configure connection timeout at the connection string level
            // This is done via the connection string builder
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(connectionString))
            {
                var builder = new NpgsqlConnectionStringBuilder(connectionString);
                builder.Timeout = 30; // Connection timeout in seconds (default is 15)
                builder.KeepAlive = 30; // Keep-alive interval in seconds
                builder.Pooling = true; // Enable connection pooling
                builder.MinPoolSize = 0;
                builder.MaxPoolSize = 100;
                
                options.UseNpgsql(builder.ConnectionString, npgsqlOptions =>
                {
                    // Increase command timeout to 60 seconds (default is 30)
                    npgsqlOptions.CommandTimeout(60);
                    
                    // Enable retry on failure for transient errors
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                });
            }
            else
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.CommandTimeout(60);
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                });
            }
            
            // Enable sensitive data logging in development only
            #if DEBUG
            options.EnableSensitiveDataLogging();
            #endif
        });

        // Register services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IServiceProviderRegistrationService, ServiceProviderRegistrationService>();
        services.AddScoped<IServiceProviderProfileService, ServiceProviderProfileService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IMasterDataService, MasterDataService>();
        services.AddScoped<IBannerService, BannerService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<ILeaveService, LeaveService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IMasterAdminService, MasterAdminService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IPdfGenerationService, PdfGenerationService>();
        services.AddScoped<INotificationService, EmailNotificationService>();

        return services;
    }
}