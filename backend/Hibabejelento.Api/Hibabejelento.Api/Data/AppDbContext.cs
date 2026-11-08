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

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<Priority> Priorities => Set<Priority>();

    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<TicketStatusHistory> TicketStatusHistory =>
        Set<TicketStatusHistory>();

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

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");

            entity.HasKey(category => category.Id);

            entity.Property(category => category.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(category => category.Name)
                .IsUnique();

            entity.Property(category => category.Description)
                .HasMaxLength(500);

            entity.Property(category => category.IsActive)
                .HasDefaultValue(true);

            entity.Property(category => category.CreatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("Locations");

            entity.HasKey(location => location.Id);

            entity.Property(location => location.Name)
                .HasMaxLength(300)
                .IsRequired();

            entity.HasIndex(location => location.Name)
                .IsUnique();

            entity.Property(location => location.Description)
                .HasMaxLength(500);

            entity.Property(location => location.IsActive)
                .HasDefaultValue(true);

            entity.Property(location => location.CreatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<Priority>(entity =>
        {
            entity.ToTable("Priorities");

            entity.HasKey(priority => priority.Id);

            entity.Property(priority => priority.Name)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(priority => priority.Name)
                .IsUnique();

            entity.Property(priority => priority.DefaultDueDays)
                .IsRequired();

            entity.Property(priority => priority.IsDefault)
                .HasDefaultValue(false);

            entity.Property(priority => priority.IsActive)
                .HasDefaultValue(true);

            entity.Property(priority => priority.CreatedAt)
                .IsRequired();
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("Tickets");

            entity.HasKey(ticket => ticket.Id);

            entity.Property(ticket => ticket.TicketNumber)
                .HasMaxLength(30)
                .IsRequired();

            entity.HasIndex(ticket => ticket.TicketNumber)
                .IsUnique();

            entity.Property(ticket => ticket.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(ticket => ticket.Description)
                .IsRequired();

            entity.Property(ticket => ticket.Location)
                .HasMaxLength(300)
                .IsRequired();

            entity.Property(ticket => ticket.Priority)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(ticket => ticket.AssetIdentifier)
                .HasMaxLength(100);

            entity.Property(ticket => ticket.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(ticket => ticket.IsActive)
                .HasDefaultValue(true);

            entity.HasIndex(ticket => ticket.LocationId);
            entity.HasIndex(ticket => ticket.PriorityId);
            entity.HasIndex(ticket => ticket.ReporterId);
            entity.HasIndex(ticket => ticket.GroupId);
            entity.HasIndex(ticket => ticket.Status);

            entity.HasOne(ticket => ticket.LocationData)
                .WithMany(location => location.Tickets)
                .HasForeignKey(ticket => ticket.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ticket => ticket.PriorityData)
                .WithMany(priority => priority.Tickets)
                .HasForeignKey(ticket => ticket.PriorityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ticket => ticket.Category)
                .WithMany()
                .HasForeignKey(ticket => ticket.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ticket => ticket.Reporter)
                .WithMany()
                .HasForeignKey(ticket => ticket.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ticket => ticket.Group)
                .WithMany()
                .HasForeignKey(ticket => ticket.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ticket => ticket.AssignedUser)
                .WithMany()
                .HasForeignKey(ticket => ticket.AssignedUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TicketStatusHistory>(entity =>
        {
            entity.ToTable("TicketStatusHistory");

            entity.HasKey(history => history.Id);

            entity.Property(history => history.OldStatus)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.Property(history => history.NewStatus)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(history => history.Reason)
                .HasMaxLength(500);

            entity.Property(history => history.CreatedAt)
                .IsRequired();

            entity.HasIndex(history => new
            {
                history.TicketId,
                history.CreatedAt
            });

            entity.HasOne(history => history.Ticket)
                .WithMany(ticket => ticket.StatusHistory)
                .HasForeignKey(history => history.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(history => history.ChangedByUser)
                .WithMany()
                .HasForeignKey(history => history.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}