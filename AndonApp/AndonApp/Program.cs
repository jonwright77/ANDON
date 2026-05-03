using AndonApp.Data;
using AndonApp.Hubs;
using AndonApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Optional settings files loaded after appsettings.json so they can override their sections.
// Environment variables are re-added last so they always win over on-disk files in production.
builder.Configuration.AddJsonFile("erp-settings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile("email-settings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// ---- Database ----
// Factory registered as singleton so Blazor Server circuits can create short-lived contexts per operation.
// Also registers AndonDbContext as scoped for controllers and services.
builder.Services.AddDbContextFactory<AndonDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---- Authentication ----
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/admin/login";
        options.LogoutPath = "/admin/logout";
        options.AccessDeniedPath = "/admin/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// ---- Services ----
builder.Services.AddSingleton<LoginAttemptTracker>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IIncidentService, IncidentService>();

// ---- ERP integration ----
builder.Services.Configure<ErpSettings>(builder.Configuration.GetSection("ErpSettings"));
builder.Services.AddSingleton<ErpPollStatus>();
builder.Services.AddTransient<IErpDataService, ErpDataService>();
builder.Services.AddHostedService<ErpPollingService>();

// ---- Razor Pages (for login/logout) ----
builder.Services.AddRazorPages();

// ---- Blazor + SignalR + API ----
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();
builder.Services.AddSignalR();

var app = builder.Build();

// ---- Seed DB ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AndonDbContext>();
    var seederLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
    await DbSeeder.SeedAsync(db, seederLogger);
}

// ---- Pipeline ----
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    ctx.Response.Headers.Append("X-Frame-Options", "DENY");
    ctx.Response.Headers.Append("Referrer-Policy", "strict-origin");
    ctx.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    // Blazor Server requires unsafe-inline for its injected scripts.
    // ws:/wss: allows the SignalR WebSocket connection.
    ctx.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "connect-src 'self' ws: wss:; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "frame-ancestors 'none'");
    await next();
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorPages();
app.MapControllers();
app.MapHub<AndonHub>("/hubs/andon");

app.MapRazorComponents<AndonApp.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
