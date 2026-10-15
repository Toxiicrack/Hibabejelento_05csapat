using Hibabejelento.Api.Middleware.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hibabejelento.Api.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Group> Groups => Set<Group>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(user => user.Email)
                .HasMaxLength(150)
                .IsRequired();

            entity.HasIndex(user => user.Email)
                .IsUnique();

            entity.Property(user => user.PasswordHash)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(user => user.RegistrationStatus)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(user => user.IsAdmin)
                .HasDefaultValue(false);

            entity.Property(user => user.IsActive)
                .HasDefaultValue(true);

            entity.Property(user => user.CreatedAt)
                .IsRequired();

            entity.HasOne(user => user.Group)
                .WithMany(group => group.Users)
                .HasForeignKey(user => user.GroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.ToTable("Groups");

            entity.HasKey(group => group.Id);

            entity.Property(group => group.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(group => group.Name)
                .IsUnique();

            entity.Property(group => group.Description)
                .HasMaxLength(500);

            entity.Property(group => group.IsActive)
                .HasDefaultValue(true);

            entity.Property(group => group.CreatedAt)
                .IsRequired();
        });
    }
}