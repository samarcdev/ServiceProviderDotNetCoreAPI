using APIServiceManagement.Application.Interfaces.Services;
using APIServiceManagement.Infrastructure.Data;
using APIServiceManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace APIServiceManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register DbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Register services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IServiceProviderRegistrationService, ServiceProviderRegistrationService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IMasterDataService, MasterDataService>();
        services.AddScoped<IBannerService, BannerService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        return services;
    }
}