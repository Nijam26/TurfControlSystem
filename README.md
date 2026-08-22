# Turf Operating & Live Control System

A single-project .NET 8 Blazor Server application that turns a turf-booking system
into a **live match/session control layer** — booking + real-time countdown +
big-screen display + automatic voice/toast announcements + payments + reports +
role-based access control + themeable UI, all in one place.

## Why one project, Clean-Architecture style

Everything ships as **one deployable** (one `.csproj`, one process, one port), but the
code inside is organized exactly like a multi-project Clean Architecture solution — the
folders *are* the layers, and the dependency rule is enforced by what each folder is
allowed to reference:

```
TurfControlSystem/
├── Domain/            → Entities, Enums. No dependencies on anything else.
│   ├── Entities/       Turf, Team, Booking, Payment, AnnouncementLog,
│   │                    AppUser, AppRole, AppPage, RolePagePermission
│   ├── Enums/          TurfType, BookingStatus, PaymentStatus, TurfLightStatus
│   └── Common/         BaseEntity
│
├── Application/       → Business rules. Depends only on Domain.
│   ├── DTOs/            TurfStatusDto, AnnouncementDto, CreateBookingRequest, DailyReportDto
│   ├── Interfaces/      ITurfRepository, IBookingRepository, ITeamRepository,
│   │                    IPaymentRepository, IAnnouncementRepository, ITimerBroadcaster,
│   │                    IUserRepository, IRoleRepository, IAppPageRepository,
│   │                    IRolePermissionRepository
│   └── Services/        BookingService, TurfService, TurfTimerService, AnnouncementService,
│                        ReportService, ReportPdfService, ReportExcelService,
│                        AuthService, PermissionService, RoleManagementService, UserManagementService
│
├── Infrastructure/    → Implements the Application interfaces. Depends on Application + Domain.
│   ├── Data/             AppDbContext (EF Core), SeedData
│   ├── Repositories/     EF Core implementations of every I*Repository
│   ├── Realtime/         TurfTimerHub (SignalR), SignalRTimerBroadcaster (implements ITimerBroadcaster)
│   └── BackgroundServices/  CountdownBackgroundService — the engine that ticks every second
│
├── Pages/ & Layout/    → Blazor Server UI (Presentation). Depends on Application only —
│                         it never touches Infrastructure or EF Core directly.
│   ├── Account/Login.cshtml   Plain Razor Page (not a Blazor component — needed for cookie sign-in)
│   └── Admin/                 Users.razor, Roles.razor, Permissions.razor
│
├── Shared/             → PagePermissionGuard.razor, ThemeSwitcher.razor — reusable
│                         components used across multiple pages
│
├── wwwroot/            → CSS (themeable via CSS variables) + JS (SignalR/speech bridge,
│                         theme engine, report date picker)
├── Program.cs          → Composition root: wires every interface to its implementation,
│                         auth middleware, and all HTTP endpoints
└── appsettings.json    → UseSqlite=true by default so it runs with zero setup
```

The dependency rule: **Domain knows nothing. Application knows only Domain. Infrastructure
implements Application's interfaces. Presentation (Pages) talks only to Application.**
Program.cs is the only place all four layers meet, via dependency injection.

## The core workflow

```
 1. Operator creates a booking (Bookings page)
        │  BookingService validates no overlap, finds/creates the Team,
        │  calculates price (peak-hour aware), saves via IBookingRepository.
        ▼
 2. CountdownBackgroundService ticks every second (Infrastructure/BackgroundServices)
        │  • BookingService.SyncBookingStatusesAsync()  → Scheduled → Running → Completed
        │  • TurfTimerService.GetTurfStatusesAsync()    → remaining time + traffic-light color
        │  • AnnouncementService.CheckAnnouncementsAsync() → fires 10/5/1/0-minute alerts once each
        ▼
 3. SignalRTimerBroadcaster pushes both events over TurfTimerHub ("/hubs/turftimer")
        ▼
 4. Every connected screen updates instantly, with zero polling:
        • Operator Dashboard  (/dashboard)  — Pause/Resume, +10 min Extend, Stop
        • TV / LED Display    (/tv-display) — big-screen countdown, speaks announcements
          (any external kiosk, smart-TV browser, or mobile client could subscribe to the
          same hub the same way — that's the point of routing everything through SignalR
          instead of wiring the UI pages directly to the database)
        ▼
 5. Reports page aggregates the day: bookings, revenue, dues, average duration —
    downloadable as PDF or Excel.
```

### Traffic-light thresholds (fixed regardless of the active UI theme)
| Remaining time | Color       | Label            |
|-----------------|------------|------------------|
| > 15 min         | 🟢 Green   | RUNNING          |
| 5–15 min         | 🟡 Yellow  | ENDING SOON      |
| < 5 min          | 🔴 Red     | TIME ALMOST UP   |
| 0 (or less)      | ⚫ Gray    | TIME UP          |

Announcements fire once per booking at 10, 5, 1, and 0 minutes remaining
(`AnnouncementLog` prevents duplicates even if the app restarts mid-session).
The TV display speaks them aloud via the browser's Web Speech API — no extra
hardware or paid TTS service needed to try it out.

## Access control — who can see what

The app now requires sign-in and is fully role-based:

- **Login** — `/Account/Login`, a plain Razor Page (not a Blazor component, since setting
  the auth cookie needs a normal HTTP request/response cycle). Cookie auth, 8-hour sliding
  expiration.
- **Roles are admin-editable, not hardcoded.** Seeded with three starters — **Admin**
  (full access, protected, can't be renamed/deleted), **Operator** (dashboard, bookings,
  turfs, TV display), **Viewer** (home, TV display, reports) — but an admin can create,
  rename, or delete any custom role from **Admin → Roles** (`/admin/roles`).
- **Users** are managed from **Admin → Users** (`/admin/users`) — create logins, assign a
  role, reset passwords, activate/deactivate. Passwords are hashed with ASP.NET Core's
  `PasswordHasher` (PBKDF2) — no plaintext, no full Identity system pulled in.
- **Page-level permissions** are a role × page checkbox matrix at **Admin → Permissions**
  (`/admin/permissions`) — tick which pages each role can see. Every page in the app
  (including the admin pages themselves) is a row in this matrix. The **Admin** role always
  passes regardless of what's checked, so a permission-matrix mistake can never lock every
  admin out.
- Every page is wrapped in `<PagePermissionGuard PageKey="...">` (`Shared/PagePermissionGuard.razor`),
  which redirects to login if unauthenticated or shows an inline "access denied" message if
  the signed-in user's role isn't granted that page. `NavMenu.razor` hides links the current
  user can't open anyway.
- **Default admin login (change this immediately):** username `admin`, password `Admin@123`.
- **`/tv-display` currently sits behind the same login as everything else.** If it needs to
  run unattended on a lobby TV, either grant a broad role TvDisplay access, or carve out a
  separate unauthenticated route for it — not done yet.
- Sign-out is a plain `GET /Account/Logout` — fine for now, worth upgrading to a
  CSRF-protected POST before production.

## Reports: PDF and Excel export

The Reports page (`/reports`) offers two download buttons for the same data:
- **PDF** — `GET /api/reports/export-pdf`, rendered via `ReportPdfService` (QuestPDF).
- **Excel** — `GET /api/reports/export-excel`, rendered via `ReportExcelService`
  (ClosedXML) — same summary + bookings table, as a formatted `.xlsx` workbook.

Both take `start`/`end` query params matching the page's date-range filter.

## Theming

The whole UI is driven by four CSS variables (`--bg`, `--panel`, `--text`, `--brand`) in
`wwwroot/css/app.css` — everything else (borders, dimmed text, hover tints, glows) is
derived from those via `color-mix()`, so a theme only ever has to set 4 values.

- **Presets**: Midnight (default), Ocean Deep, Ember Noir, Violet Nightfall, Slate Daylight
  (a light theme) — switch via `[data-theme="..."]` on `<html>`.
- **Custom themes**: pick your own Background/Panel/Text/Accent colors from a color picker,
  either from the sidebar's **🎨 Theme** button (`Shared/ThemeSwitcher.razor`) or the
  dedicated `/theme` page.
- Applied instantly and saved to `localStorage` (`wwwroot/js/theme.js`) — persists per
  browser, not per account. The theme script runs synchronously in `<head>` on both the
  main app shell and the login page, so there's no flash of the wrong theme on load.
- **Turf-status colors (green/yellow/red) are intentionally NOT themeable** — they're
  safety-relevant traffic-light semantics and stay fixed regardless of the active theme.
- The `/tv-display` kiosk screen also stays fixed (bold black stadium look) rather than
  taking on an admin's personal theme choice, aside from a faint brand-color tint in its
  background glow.

## Running it

**Visual Studio:** open `TurfControlSystem.sln`, set `TurfControlSystem` as the startup
project (it's the only one), and press F5 / Ctrl+F5.

**Command line:**
```bash
cd TurfControlSystem
dotnet restore
dotnet run
```

By default `appsettings.json` has `"UseSqlite": true`, so the app creates a local
`turfcontrol.db` file on first run (via `Database.EnsureCreated()`) and seeds it with
3 demo turfs, 6 demo teams, a few live/upcoming bookings, the starter roles/pages/permission
matrix, and the default `admin` login. Sign in and open `/dashboard` or `/tv-display` right
away to watch the countdowns run.

To point it at SQL Server instead, set `"UseSqlite": false` and fill in
`ConnectionStrings:DefaultConnection` with your own server's address. **Don't commit
real credentials** — put them in `appsettings.Development.json` (already git-ignored) or
use `dotnet user-secrets`:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True;"
```

Either way, `EnsureCreated()` + `SeedData.Seed()` will create the database, every table,
and the starter roles/users/permissions automatically on first run — no manual SQL needed.

Once you're ready to move off `EnsureCreated()`, switch to real EF Core migrations:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Pages

| Route                 | Purpose | Default access |
|------------------------|---------|-----------------|
| `/`                     | Landing page with quick links | Home, Dashboard, TV, Bookings |
| `/dashboard`            | Operator control panel — live countdown per turf, Pause/Resume, +10 min, Stop | Admin, Operator |
| `/tv-display`           | Full-screen, read-only display for a TV/LED screen in the facility | Admin, Operator, Viewer |
| `/bookings`             | Full CRUD for bookings — create (auto-detects returning teams by mobile), edit an upcoming booking's turf/time/players/advance, cancel, or delete (only if it hasn't started or was cancelled) | Admin, Operator |
| `/turfs`                | Full CRUD for turfs — add, edit, activate/deactivate, delete (a turf with booking history can't be hard-deleted; deactivate it instead) | Admin, Operator |
| `/reports`              | Daily report — total bookings, cancellations, revenue, dues, average duration, PDF/Excel export | Admin, Viewer |
| `/theme`                | Full-page theme picker (presets + custom color builder) | Anyone signed in |
| `/admin/users`          | Create logins, assign roles, reset passwords | Admin only |
| `/admin/roles`          | Create/rename/delete roles | Admin only |
| `/admin/permissions`    | Role × page permission matrix | Admin only |
| `/Account/Login`        | Sign in | Public |

Access per role is admin-configurable at `/admin/permissions` — the table above reflects
the seeded defaults, not a hard limit.

## Known simplifications — call these out before production

This is a working MVP, not a production build. Before deploying for real:

- **Logout is a plain GET, not a CSRF-protected POST.** Fine for now, worth hardening.
- **`/tv-display` has no separate unauthenticated boundary** — since it's meant to run
  unattended on a lobby TV, consider a network-restricted, unauthenticated route for it in
  production rather than gating it behind the same login as the admin dashboard.
- **Peak-hour rule is a placeholder** (5 PM–10 PM, flat). Swap `BookingService.IsPeakHour`
  for real per-turf peak windows if pricing needs to vary by day or turf.
- **Booking edit/delete is intentionally restricted** — you can only edit or delete a
  booking that hasn't started yet (or was cancelled); once a session is Running the
  Dashboard's pause/extend/stop are the only controls, and Completed bookings are frozen
  as history for Reports. There's currently no "reschedule a live session" flow beyond
  Pause + Extend.
- **SQLite is for local demo/dev only** — switch to SQL Server for anything multi-user or
  production, and move credentials out of any file that gets committed.
- **Theme choice is per-browser (`localStorage`), not per-account** — signing in on a
  different device won't carry a saved custom theme with it.
- **Custom theme colors aren't validated for contrast** — nothing stops a user from picking
  a background and text color that are hard to read against each other.