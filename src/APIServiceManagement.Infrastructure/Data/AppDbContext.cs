using APIServiceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace APIServiceManagement.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
 
    public DbSet<AdminStateAssignment> AdminStateAssignments { get; set; }
    public DbSet<Banner> Banners { get; set; }
    public DbSet<BookingRequest> BookingRequests { get; set; }
    public DbSet<BookingStatusHistory> BookingStatusHistories { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<ProviderService> ProviderServices { get; set; }
    public DbSet<RoleTermsCondition> RoleTermsConditions { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<ServiceAvailablePincode> ServiceAvailablePincodes { get; set; }
    public DbSet<ServicePrice> ServicePrices { get; set; }
    public DbSet<ServiceProviderVerification> ServiceProviderVerifications { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<State> States { get; set; }
    public DbSet<TermsAndCondition> TermsAndConditions { get; set; }
    public DbSet<UserPincodePreference> UserPincodePreferences { get; set; }
    public DbSet<UserRegistrationStep> UserRegistrationSteps { get; set; }
    public DbSet<UsersAddress> UsersAddresses { get; set; }
    public DbSet<UsersExtraInfo> UsersExtraInfos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

         
    }
}