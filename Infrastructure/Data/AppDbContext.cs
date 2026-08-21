namespace TurfControlSystem.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Turf> Turfs => Set<Turf>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AnnouncementLog> AnnouncementLogs => Set<AnnouncementLog>();

    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AppRole> AppRoles => Set<AppRole>();
    public DbSet<AppPage> AppPages => Set<AppPage>();
    public DbSet<RolePagePermission> RolePagePermissions => Set<RolePagePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Turf>(e =>
        {
            e.Property(t => t.Name).IsRequired().HasMaxLength(100);
            e.Property(t => t.HourlyPrice).HasColumnType("decimal(10,2)");
            e.Property(t => t.PeakHourPrice).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<Team>(e =>
        {
            e.Property(t => t.Name).IsRequired().HasMaxLength(100);
            e.Property(t => t.MobileNumber).IsRequired().HasMaxLength(20);
            e.HasIndex(t => t.MobileNumber);
        });

        modelBuilder.Entity<Booking>(e =>
        {
            e.Property(b => b.TotalAmount).HasColumnType("decimal(10,2)");
            e.Property(b => b.AdvancePayment).HasColumnType("decimal(10,2)");

            e.HasOne(b => b.Turf)
                .WithMany(t => t.Bookings)
                .HasForeignKey(b => b.TurfId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(b => b.Team)
                .WithMany(t => t.Bookings)
                .HasForeignKey(b => b.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(b => new { b.TurfId, b.StartTime, b.EndTime });
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.Property(p => p.Amount).HasColumnType("decimal(10,2)");

            e.HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AnnouncementLog>(e =>
        {
            e.HasIndex(a => new { a.BookingId, a.ThresholdMinutes }).IsUnique();

            // No navigation property on either side, but still a real FK so deleting a
            // booking cleans up its announcement log rows instead of leaving orphans.
            e.HasOne<Booking>()
                .WithMany()
                .HasForeignKey(a => a.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppRole>(e =>
        {
            e.Property(r => r.Name).IsRequired().HasMaxLength(50);
            e.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<AppUser>(e =>
        {
            e.Property(u => u.Username).IsRequired().HasMaxLength(50);
            e.HasIndex(u => u.Username).IsUnique();

            e.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppPage>(e =>
        {
            e.Property(p => p.Key).IsRequired().HasMaxLength(50);
            e.HasIndex(p => p.Key).IsUnique();
        });

        modelBuilder.Entity<RolePagePermission>(e =>
        {
            e.HasIndex(p => new { p.RoleId, p.PageId }).IsUnique();

            e.HasOne(p => p.Role)
                .WithMany(r => r.Permissions)
                .HasForeignKey(p => p.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.Page)
                .WithMany(pg => pg.Permissions)
                .HasForeignKey(p => p.PageId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
