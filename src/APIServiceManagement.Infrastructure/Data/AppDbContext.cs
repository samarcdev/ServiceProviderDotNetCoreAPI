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
    public DbSet<BookingStatus> BookingStatuses { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<CityPincode> CityPincodes { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<ProviderService> ProviderServices { get; set; }
    public DbSet<RoleTermsCondition> RoleTermsConditions { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<ServicePrice> ServicePrices { get; set; }
    public DbSet<ServiceProviderVerification> ServiceProviderVerifications { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<ServiceType> ServiceTypes { get; set; }
    public DbSet<State> States { get; set; }
    public DbSet<TermsAndCondition> TermsAndConditions { get; set; }
    public DbSet<CustomerPincodePreference> CustomerPincodePreferences { get; set; }
    public DbSet<ServiceProviderPincodePreference> ServiceProviderPincodePreferences { get; set; }
    public DbSet<UserRegistrationStep> UserRegistrationSteps { get; set; }
    public DbSet<UsersAddress> UsersAddresses { get; set; }
    public DbSet<UsersExtraInfo> UsersExtraInfos { get; set; }
    public DbSet<UserStatus> UserStatuses { get; set; }
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

        modelBuilder.Entity<User>()
            .HasOne(user => user.Status)
            .WithMany()
            .HasForeignKey(user => user.StatusId);

        modelBuilder.Entity<User>()
            .HasOne(user => user.VerificationStatus)
            .WithMany(status => status.Users)
            .HasForeignKey(user => user.VerificationStatusId);

        // Configure CustomerPincodePreference table - table name: customer_pincode_preferences
        modelBuilder.Entity<CustomerPincodePreference>(entity =>
        {
            entity.ToTable("customer_pincode_preferences");
            
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");
            
            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();
            
            entity.Property(e => e.Pincode)
                .HasColumnName("pincode")
                .HasMaxLength(6)
                .IsRequired();
            
            entity.Property(e => e.IsPrimary)
                .HasColumnName("is_primary")
                .HasDefaultValue(false);
            
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // Foreign key relationship
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Unique constraint: one user can have only one entry per pincode
            entity.HasIndex(e => new { e.UserId, e.Pincode })
                .IsUnique()
                .HasDatabaseName("UQ_CustomerPincodePreferences_User_Pincode");
        });

        // Configure ServiceProviderPincodePreference table - table name: service_provider_pincode_preferences
        modelBuilder.Entity<ServiceProviderPincodePreference>(entity =>
        {
            entity.ToTable("service_provider_pincode_preferences");
            
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");
            
            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();
            
            entity.Property(e => e.Pincode)
                .HasColumnName("pincode")
                .HasMaxLength(6)
                .IsRequired();
            
            entity.Property(e => e.IsPrimary)
                .HasColumnName("is_primary")
                .HasDefaultValue(false);
            
            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // Foreign key relationship
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Unique constraint: one service provider can have only one entry per pincode
            entity.HasIndex(e => new { e.UserId, e.Pincode })
                .IsUnique()
                .HasDatabaseName("UQ_ServiceProviderPincodePreferences_User_Pincode");
        });

        // Configure ServiceType relationship with BookingRequest
        modelBuilder.Entity<BookingRequest>()
            .HasOne(booking => booking.ServiceType)
            .WithMany(serviceType => serviceType.BookingRequests)
            .HasForeignKey(booking => booking.ServiceTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure ServiceType relationship with Service
        modelBuilder.Entity<ServiceType>()
            .HasOne(st => st.Service)
            .WithMany()
            .HasForeignKey(st => st.ServiceId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure BookingStatus relationships
        modelBuilder.Entity<BookingRequest>()
            .HasOne(booking => booking.StatusNavigation)
            .WithMany(status => status.BookingRequests)
            .HasForeignKey(booking => booking.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BookingStatusHistory>()
            .HasOne(history => history.StatusNavigation)
            .WithMany(status => status.BookingStatusHistories)
            .HasForeignKey(history => history.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

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