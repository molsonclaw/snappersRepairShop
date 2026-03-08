using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SnappersRepairShop.Shared.Models;

namespace SnappersRepairShop.Data;

/// <summary>
/// Application database context with Identity integration
/// Manages all entities and relationships for the repair shop
/// </summary>
public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        // Suppress pending model changes warning for development
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    // DbSets for all entities
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Vehicle> Vehicles { get; set; } = null!;
    public DbSet<WorkOrder> WorkOrders { get; set; } = null!;
    public DbSet<LaborLine> LaborLines { get; set; } = null!;
    public DbSet<PartUsed> PartsUsed { get; set; } = null!;
    public DbSet<JobPhoto> JobPhotos { get; set; } = null!;
    public DbSet<AppSettings> AppSettings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Customer entity
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(e => e.Phone);
            entity.HasIndex(e => e.Email);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure Vehicle entity
        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasIndex(e => e.VIN).IsUnique();
            entity.HasIndex(e => e.LicensePlate);
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");

            // Relationship: Vehicle belongs to Customer
            entity.HasOne(v => v.Customer)
                .WithMany(c => c.Vehicles)
                .HasForeignKey(v => v.CustomerId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete
        });

        // Configure WorkOrder entity
        modelBuilder.Entity<WorkOrder>(entity =>
        {
            entity.HasIndex(e => e.WorkOrderNumber).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DateCreated);
            entity.Property(e => e.DateCreated).HasDefaultValueSql("GETUTCDATE()");

            // Relationship: WorkOrder belongs to Customer
            entity.HasOne(w => w.Customer)
                .WithMany(c => c.WorkOrders)
                .HasForeignKey(w => w.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: WorkOrder belongs to Vehicle
            entity.HasOne(w => w.Vehicle)
                .WithMany(v => v.WorkOrders)
                .HasForeignKey(w => w.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure LaborLine entity
        modelBuilder.Entity<LaborLine>(entity =>
        {
            entity.Property(e => e.DateAdded).HasDefaultValueSql("GETUTCDATE()");

            // Relationship: LaborLine belongs to WorkOrder
            entity.HasOne(l => l.WorkOrder)
                .WithMany(w => w.LaborLines)
                .HasForeignKey(l => l.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade); // Delete labor lines when work order is deleted
        });

        // Configure PartUsed entity
        modelBuilder.Entity<PartUsed>(entity =>
        {
            entity.Property(e => e.DateAdded).HasDefaultValueSql("GETUTCDATE()");

            // Relationship: PartUsed belongs to WorkOrder
            entity.HasOne(p => p.WorkOrder)
                .WithMany(w => w.PartsUsed)
                .HasForeignKey(p => p.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade); // Delete parts when work order is deleted
        });

        // Configure JobPhoto entity
        modelBuilder.Entity<JobPhoto>(entity =>
        {
            entity.HasIndex(e => e.BlobName).IsUnique();
            entity.Property(e => e.UploadedDate).HasDefaultValueSql("GETUTCDATE()");

            // Relationship: JobPhoto belongs to WorkOrder
            entity.HasOne(j => j.WorkOrder)
                .WithMany(w => w.JobPhotos)
                .HasForeignKey(j => j.WorkOrderId)
                .OnDelete(DeleteBehavior.Cascade); // Delete photos when work order is deleted
        });

        // Configure AppSettings as singleton
        modelBuilder.Entity<AppSettings>(entity =>
        {
            entity.Property(e => e.LastModified).HasDefaultValueSql("GETUTCDATE()");

            // Seed the singleton settings row
            entity.HasData(new AppSettings
            {
                AppSettingsId = 1,
                BillingEnabled = false,
                ShopName = "Snappers Repair Shop",
                ShopAddress = "Fountain City, WI",
                TaxRate = 0.055m,
                DefaultLaborRate = 95.00m,
                LastModified = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        });

        // Seed Identity Roles
        SeedRoles(modelBuilder);

        // Seed Default Admin User
        SeedDefaultAdminUser(modelBuilder);
    }

    /// <summary>
    /// Seeds the three application roles: Admin, Technician, Office
    /// </summary>
    private void SeedRoles(ModelBuilder modelBuilder)
    {
        var adminRoleId = "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d";
        var technicianRoleId = "2b3c4d5e-6f7a-8b9c-0d1e-2f3a4b5c6d7e";
        var officeRoleId = "3c4d5e6f-7a8b-9c0d-1e2f-3a4b5c6d7e8f";

        modelBuilder.Entity<IdentityRole>().HasData(
            new IdentityRole
            {
                Id = adminRoleId,
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = adminRoleId
            },
            new IdentityRole
            {
                Id = technicianRoleId,
                Name = "Technician",
                NormalizedName = "TECHNICIAN",
                ConcurrencyStamp = technicianRoleId
            },
            new IdentityRole
            {
                Id = officeRoleId,
                Name = "Office",
                NormalizedName = "OFFICE",
                ConcurrencyStamp = officeRoleId
            }
        );
    }

    /// <summary>
    /// Seeds a default admin user: admin@snappersrepair.com / Admin@123
    /// </summary>
    private void SeedDefaultAdminUser(ModelBuilder modelBuilder)
    {
        var adminUserId = "9a8b7c6d-5e4f-3a2b-1c0d-9e8f7a6b5c4d";
        var adminRoleId = "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d";

        // Create password hasher
        var hasher = new PasswordHasher<IdentityUser>();

        // Create default admin user
        var adminUser = new IdentityUser
        {
            Id = adminUserId,
            UserName = "admin@snappersrepair.com",
            NormalizedUserName = "ADMIN@SNAPPERSREPAIR.COM",
            Email = "admin@snappersrepair.com",
            NormalizedEmail = "ADMIN@SNAPPERSREPAIR.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("D"),
            ConcurrencyStamp = Guid.NewGuid().ToString("D")
        };

        // Hash the password
        adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin@123");

        // Seed the user
        modelBuilder.Entity<IdentityUser>().HasData(adminUser);

        // Assign Admin role to the user
        modelBuilder.Entity<IdentityUserRole<string>>().HasData(
            new IdentityUserRole<string>
            {
                RoleId = adminRoleId,
                UserId = adminUserId
            }
        );
    }
}

