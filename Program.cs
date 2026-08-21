using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.SignalR;
using TurfControlSystem.Application.Interfaces;
using TurfControlSystem.Application.Services;
using TurfControlSystem.Infrastructure.BackgroundServices;
using TurfControlSystem.Infrastructure.Data;
using TurfControlSystem.Infrastructure.Realtime;
using TurfControlSystem.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ---- Presentation (Blazor Server + SignalR) ----
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSignalR();

// ---- Auth: cookie sign-in, established via the Pages/Account/Login Razor Page (not a
// Blazor component — a Blazor circuit is already mid-response by the time a component
// runs, so it can't set the Set-Cookie header a normal HTTP POST can). Once the cookie is
// set, Blazor Server components read the identity from HttpContext.User via the classic
// CascadingAuthenticationState wired up in App.razor. ----
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// ---- Infrastructure: persistence ----
// UseSqlite=true (appsettings.json default) means the app runs out of the box with a
// local turfcontrol.db file — no SQL Server install required. Flip it to false and set
// DefaultConnection to point at your own SQL Server instance for production.
var useSqlite = builder.Configuration.GetValue<bool>("UseSqlite");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (useSqlite)
        options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection"));
    else
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<ITurfRepository, TurfRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IAppPageRepository, AppPageRepository>();
builder.Services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();

// ---- Infrastructure: real-time push ----
builder.Services.AddScoped<ITimerBroadcaster, SignalRTimerBroadcaster>();

// ---- Application services ----
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<TurfService>();
builder.Services.AddScoped<TurfTimerService>();
builder.Services.AddScoped<AnnouncementService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<ReportPdfService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<RoleManagementService>();
builder.Services.AddScoped<UserManagementService>();

// ---- Background engine: the live countdown "heart" of the system ----
builder.Services.AddHostedService<CountdownBackgroundService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Authentication before authorization, both between UseRouting and endpoint mapping.
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapBlazorHub();
app.MapHub<TurfTimerHub>("/hubs/turftimer");

// Plain GET so a nav link can trigger it directly (no Blazor circuit involved) — signs the
// cookie out and sends the browser back to the login page. Simple by design, matching the
// rest of this MVP's auth: a POST+antiforgery-token version is the production upgrade.
app.MapGet("/Account/Logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/Account/Login");
});

// ---- Reports: PDF download ----
// A plain GET endpoint (not a Blazor event) so the browser can navigate/download directly,
// e.g. /api/reports/export-pdf?start=2026-08-01&end=2026-08-07
app.MapGet("/api/reports/export-pdf", async (DateTime start, DateTime? end, ReportService reportService, ReportPdfService pdfService) =>
{
    var report = await reportService.GetReportAsync(start, end ?? start);
    var pdfBytes = pdfService.GeneratePdf(report);

    var fileName = report.IsSingleDay
        ? $"TurfReport_{report.StartDate:yyyyMMdd}.pdf"
        : $"TurfReport_{report.StartDate:yyyyMMdd}-{report.EndDate:yyyyMMdd}.pdf";

    return Results.File(pdfBytes, "application/pdf", fileName);
});

app.MapFallbackToPage("/_Host");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    SeedData.Seed(db);
}

app.Run();
