using APIServiceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APIServiceManagement.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
 
    public DbSet<User> Users { get; set; }
    public DbSet<AdminStateAssignment> AdminStateAssignments { get; set; }
    public DbSet<Banner> Banners { get; set; }
    public DbSet<BookingRequest> BookingRequests { get; set; }
    public DbSet<BookingStatusHistory> BookingStatusHistories { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<CityPincode> CityPincodes { get; set; }
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
    public DbSet<VerificationStatus> VerificationStatuses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProviderService>()
            .HasKey(providerService => new { providerService.UserId, providerService.ServiceId });

        modelBuilder.Entity<RoleTermsCondition>()
            .HasKey(roleTermsCondition => new { roleTermsCondition.RoleId, roleTermsCondition.TermsConditionsId });

        modelBuilder.Entity<RoleTermsCondition>()
            .HasOne(roleTermsCondition => roleTermsCondition.Role)
            .WithMany(role => role.RoleTermsConditions)
            .HasForeignKey(roleTermsCondition => roleTermsCondition.RoleId);

        modelBuilder.Entity<RoleTermsCondition>()
            .HasOne(roleTermsCondition => roleTermsCondition.TermsAndCondition)
            .WithMany(termsAndCondition => termsAndCondition.RoleTermsConditions)
            .HasForeignKey(roleTermsCondition => roleTermsCondition.TermsConditionsId);

        modelBuilder.Entity<ProviderService>()
            .HasOne(providerService => providerService.Service)
            .WithMany(service => service.ProviderServices)
            .HasForeignKey(providerService => providerService.ServiceId);

        modelBuilder.Entity<ProviderService>()
            .HasOne(providerService => providerService.User)
            .WithMany()
            .HasForeignKey(providerService => providerService.UserId);

        modelBuilder.Entity<ServiceProviderVerification>()
            .HasOne(verification => verification.ProviderUser)
            .WithMany()
            .HasForeignKey(verification => verification.ProviderUserId);

        modelBuilder.Entity<ServiceProviderVerification>()
            .HasOne(verification => verification.AssignedAdmin)
            .WithMany()
            .HasForeignKey(verification => verification.AssignedAdminId);

        modelBuilder.Entity<ServiceProviderVerification>()
            .HasOne(verification => verification.VerifiedByUser)
            .WithMany()
            .HasForeignKey(verification => verification.VerifiedBy);

        modelBuilder.Entity<ServiceProviderVerification>()
            .HasOne(verification => verification.VerificationStatus)
            .WithMany()
            .HasForeignKey(verification => verification.VerificationStatusId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Properties.Any(p => p.Metadata.Name == "CreatedAt"))
                {
                    entry.Property("CreatedAt").CurrentValue = utcNow;
                }
                if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                {
                    entry.Property("UpdatedAt").CurrentValue = utcNow;
                }
            }
            if (entry.State == EntityState.Modified)
            {
                if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                {
                    entry.Property("UpdatedAt").CurrentValue = utcNow;
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}