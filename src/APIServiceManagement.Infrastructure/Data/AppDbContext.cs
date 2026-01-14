using APIServiceManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace APIServiceManagement.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed initial data
        modelBuilder.Entity<User>().HasData(
            new User { Id = Guid.NewGuid(), Name = "John Doe", Email = "john.doe@example.com", Status = Domain.Enums.UserStatus.Active, CreatedAt = DateTime.UtcNow },
            new User { Id = Guid.NewGuid(), Name = "Jane Smith", Email = "jane.smith@example.com", Status = Domain.Enums.UserStatus.Inactive, CreatedAt = DateTime.UtcNow }
        );
    }
}