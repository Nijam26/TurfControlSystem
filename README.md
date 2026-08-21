# Turf Operating & Live Control System

A single-project .NET 8 Blazor Server application that turns a turf-booking system
into a **live match/session control layer** — the differentiator described in the
original idea: booking + real-time countdown + big-screen display + automatic
voice/toast announcements + payments + reports, all in one place.

## Why one project, Clean-Architecture style

Everything ships as **one deployable** (one `.csproj`, one process, one port), but the
code inside is organized exactly like a multi-project Clean Architecture solution — the
folders *are* the layers, and the dependency rule is enforced by what each folder is
allowed to reference:

```
TurfControlSystem/
├── Domain/            → Entities, Enums. No dependencies on anything else.
│   ├── Entities/       Turf, Team, Booking, Payment, AnnouncementLog
│   ├── Enums/          TurfType, BookingStatus, PaymentStatus, TurfLightStatus
│   └── Common/         BaseEntity
│
├── Application/       → Business rules. Depends only on Domain.
│   ├── DTOs/            TurfStatusDto, AnnouncementDto, CreateBookingRequest, DailyReportDto
│   ├── Interfaces/      ITurfRepository, IBookingRepository, ITeamRepository,
│   │                    IPaymentRepository, IAnnouncementRepository, ITimerBroadcaster
│   └── Services/        BookingService, TurfService, TurfTimerService, AnnouncementService, ReportService
│
├── Infrastructure/    → Implements the Application interfaces. Depends on Application + Domain.
│   ├── Data/             AppDbContext (EF Core), SeedData
│   ├── Repositories/     EF Core implementations of every I*Repository
│   ├── Realtime/         TurfTimerHub (SignalR), SignalRTimerBroadcaster (implements ITimerBroadcaster)
│   └── BackgroundServices/  CountdownBackgroundService — the engine that ticks every second
│
├── Pages/ & Layout/    → Blazor Server UI (Presentation). Depends on Application only —
│                         it never touches Infrastructure or EF Core directly.
│
├── wwwroot/            → CSS + the small SignalR/speech JS bridge (turfTimer.js)
├── Program.cs          → Composition root: wires every interface to its implementation
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
 5. Reports page aggregates the day: bookings, revenue, dues, average duration.
```

### Traffic-light thresholds (matches the original idea)
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
3 demo turfs, 6 demo teams, and a few live/upcoming bookings — open `/dashboard` or
`/tv-display` right away and watch the countdowns run.

To point it at SQL Server instead, set `"UseSqlite": false` and fill in
`ConnectionStrings:DefaultConnection` with your own server's address. **Don't commit
real credentials** — use `dotnet user-secrets` in development:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True;"
```

Once you're ready to move off `EnsureCreated()`, switch to real EF Core migrations:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Pages

| Route          | Purpose |
|-----------------|---------|
| `/`              | Landing page with quick links |
| `/dashboard`     | Operator control panel — live countdown per turf, Pause/Resume, +10 min, Stop |
| `/tv-display`    | Full-screen, read-only display for a TV/LED screen in the facility |
| `/bookings`      | Full CRUD for bookings — create (auto-detects returning teams by mobile), edit an upcoming booking's turf/time/players/advance, cancel, or delete (only if it hasn't started or was cancelled) |
| `/turfs`         | Full CRUD for turfs — add, edit, activate/deactivate, delete (a turf with booking history can't be hard-deleted; deactivate it instead) |
| `/reports`       | Daily report — total bookings, cancellations, revenue, dues, average duration |

## Known simplifications — call these out before production

This is a working MVP, not a production build. Before deploying for real:

- **No authentication yet.** Every page is open. Given the HORP reporting endpoints
  already have an open issue about missing/bypassed token auth, don't repeat that here —
  add ASP.NET Core Identity or JWT auth (`nijam-aspnetcore` → `security.md`) and put
  `[Authorize]` on the operator/admin pages before this touches real customers or money.
- **Peak-hour rule is a placeholder** (5 PM–10 PM, flat). Swap `BookingService.IsPeakHour`
  for real per-turf peak windows if pricing needs to vary by day or turf.
- **Booking edit/delete is intentionally restricted** — you can only edit or delete a
  booking that hasn't started yet (or was cancelled); once a session is Running the
  Dashboard's pause/extend/stop are the only controls, and Completed bookings are frozen
  as history for Reports. There's currently no "reschedule a live session" flow beyond
  Pause + Extend.
- **SQLite is for local demo/dev only** — switch to SQL Server (matching HORP's `QTCon`
  convention) for anything multi-user or production.
- **The `/tv-display` page has no auth boundary** — since it's meant to run unattended
  on a lobby TV, consider putting it behind a separate unauthenticated-but-network-restricted
  route in production rather than the same host as the admin dashboard.
