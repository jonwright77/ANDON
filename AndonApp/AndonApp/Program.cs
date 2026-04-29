using AndonApp.Data;
using AndonApp.Hubs;
using AndonApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// ---- Services ----
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IIncidentService, IncidentService>();

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
    await DbSeeder.SeedAsync(db);
}

// ---- Pipeline ----
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
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
