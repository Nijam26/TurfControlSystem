using Microsoft.AspNetCore.Identity;

namespace TurfControlSystem.Infrastructure.Data;

/// <summary>Drops in demo turfs, teams and a few live bookings so the app is useful on first run.</summary>
public static class SeedData
{
    public static void Seed(AppDbContext db)
    {
        SeedAccessControl(db);

        if (db.Turfs.Any()) return;

        var turf1 = new Turf { Name = "Turf 1", Type = TurfType.FiveASide, IsIndoor = false, HourlyPrice = 1500, PeakHourPrice = 2000 };
        var turf2 = new Turf { Name = "Turf 2", Type = TurfType.SevenASide, IsIndoor = false, HourlyPrice = 2000, PeakHourPrice = 2500 };
        var turf3 = new Turf { Name = "Turf 3", Type = TurfType.FiveASide, IsIndoor = true, HourlyPrice = 1800, PeakHourPrice = 2300 };

        db.Turfs.AddRange(turf1, turf2, turf3);
        db.SaveChanges();

        var eagles = new Team { Name = "Eagles FC", CaptainName = "Rahim", MobileNumber = "01711000001" };
        var warriors = new Team { Name = "Warriors", CaptainName = "Karim", MobileNumber = "01711000002" };
        var tigers = new Team { Name = "Tigers", CaptainName = "Jamal", MobileNumber = "01711000003" };
        var redDragons = new Team { Name = "Red Dragons FC", CaptainName = "Nasir", MobileNumber = "01711000004" };
        var unitedFc = new Team { Name = "United FC", CaptainName = "Sabbir", MobileNumber = "01711000005" };
        var falcons = new Team { Name = "Falcons", CaptainName = "Farhan", MobileNumber = "01711000006" };

        db.Teams.AddRange(eagles, warriors, tigers, redDragons, unitedFc, falcons);
        db.SaveChanges();

        var now = DateTime.Now;

        db.Bookings.AddRange(
            new Booking
            {
                TurfId = turf1.Id, TeamId = eagles.Id, NumberOfPlayers = 5,
                StartTime = now.AddMinutes(-42), EndTime = now.AddMinutes(18), OriginalEndTime = now.AddMinutes(18),
                TotalAmount = 1500, AdvancePayment = 500, Status = BookingStatus.Running
            },
            new Booking
            {
                TurfId = turf1.Id, TeamId = redDragons.Id, NumberOfPlayers = 5,
                StartTime = now.AddMinutes(18), EndTime = now.AddMinutes(78), OriginalEndTime = now.AddMinutes(78),
                TotalAmount = 1500, AdvancePayment = 300, Status = BookingStatus.Scheduled
            },
            new Booking
            {
                TurfId = turf2.Id, TeamId = warriors.Id, NumberOfPlayers = 7,
                StartTime = now.AddMinutes(-12), EndTime = now.AddMinutes(48), OriginalEndTime = now.AddMinutes(48),
                TotalAmount = 2000, AdvancePayment = 1000, Status = BookingStatus.Running
            },
            new Booking
            {
                TurfId = turf2.Id, TeamId = unitedFc.Id, NumberOfPlayers = 7,
                StartTime = now.AddMinutes(48), EndTime = now.AddMinutes(108), OriginalEndTime = now.AddMinutes(108),
                TotalAmount = 2000, AdvancePayment = 0, Status = BookingStatus.Scheduled
            },
            new Booking
            {
                TurfId = turf3.Id, TeamId = tigers.Id, NumberOfPlayers = 5,
                StartTime = now.AddMinutes(-57), EndTime = now.AddMinutes(3), OriginalEndTime = now.AddMinutes(3),
                TotalAmount = 1800, AdvancePayment = 1800, Status = BookingStatus.Running
            },
            new Booking
            {
                TurfId = turf3.Id, TeamId = falcons.Id, NumberOfPlayers = 5,
                StartTime = now.AddMinutes(3), EndTime = now.AddMinutes(63), OriginalEndTime = now.AddMinutes(63),
                TotalAmount = 1800, AdvancePayment = 500, Status = BookingStatus.Scheduled
            }
        );

        db.SaveChanges();
    }

    /// <summary>
    /// Seeds the page catalog, three starter roles (Admin/Operator/Viewer), a default
    /// admin login, and a starter permission matrix. Runs every startup (not just on an
    /// empty DB) so a page added in code later still shows up in the catalog automatically.
    /// </summary>
    private static void SeedAccessControl(AppDbContext db)
    {
        var pageDefs = new (string Key, string DisplayName, string Route, string Icon, int Sort)[]
        {
            ("Home", "Home", "", "🏠", 0),
            ("Dashboard", "Operator Dashboard", "dashboard", "🎛️", 1),
            ("TvDisplay", "TV / LED Display", "tv-display", "📺", 2),
            ("Bookings", "Bookings", "bookings", "📅", 3),
            ("Turfs", "Turfs", "turfs", "🏟️", 4),
            ("Reports", "Reports", "reports", "📊", 5),
            ("AdminUsers", "Admin · Users", "admin/users", "👤", 6),
            ("AdminRoles", "Admin · Roles", "admin/roles", "🛡️", 7),
            ("AdminPermissions", "Admin · Permissions", "admin/permissions", "🔐", 8),
        };

        foreach (var def in pageDefs)
        {
            var existing = db.AppPages.FirstOrDefault(p => p.Key == def.Key);
            if (existing is null)
            {
                db.AppPages.Add(new AppPage
                {
                    Key = def.Key,
                    DisplayName = def.DisplayName,
                    Route = def.Route,
                    Icon = def.Icon,
                    SortOrder = def.Sort
                });
            }
            else
            {
                existing.DisplayName = def.DisplayName;
                existing.Route = def.Route;
                existing.Icon = def.Icon;
                existing.SortOrder = def.Sort;
            }
        }
        db.SaveChanges();

        if (db.AppRoles.Any()) return; // roles/users/permissions only need to be seeded once

        var admin = new AppRole { Name = "Admin", Description = "Full access to every page, including user/role/permission management.", IsSystemRole = true };
        var operatorRole = new AppRole { Name = "Operator", Description = "Day-to-day front-desk access: dashboard, bookings, turfs, TV display." };
        var viewer = new AppRole { Name = "Viewer", Description = "Read-only access: home, TV display, and reports." };

        db.AppRoles.AddRange(admin, operatorRole, viewer);
        db.SaveChanges();

        var pages = db.AppPages.ToList();
        AppPage P(string key) => pages.First(p => p.Key == key);

        // Admin doesn't strictly need rows (PermissionService always grants it full access),
        // but seeding them keeps the Admin → Permissions matrix visually accurate from day one.
        foreach (var page in pages)
            db.RolePagePermissions.Add(new RolePagePermission { RoleId = admin.Id, PageId = page.Id, CanView = true });

        foreach (var key in new[] { "Home", "Dashboard", "TvDisplay", "Bookings", "Turfs" })
            db.RolePagePermissions.Add(new RolePagePermission { RoleId = operatorRole.Id, PageId = P(key).Id, CanView = true });

        foreach (var key in new[] { "Home", "TvDisplay", "Reports" })
            db.RolePagePermissions.Add(new RolePagePermission { RoleId = viewer.Id, PageId = P(key).Id, CanView = true });

        db.SaveChanges();

        // Default admin login — change this password after first sign-in (Admin → Users → Reset Password).
        var adminUser = new AppUser
        {
            Username = "admin",
            DisplayName = "Administrator",
            RoleId = admin.Id,
            IsActive = true
        };
        var hasher = new PasswordHasher<AppUser>();
        adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin@123");

        db.AppUsers.Add(adminUser);
        db.SaveChanges();
    }
}
