using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MovieCatalog.Data;
using MovieCatalog.Hubs;
using MovieCatalog.Models;
using MovieCatalog.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Database: SQLite file next to the app, created automatically ----------
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "moviecatalog.db");
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));

// ---------- Authentication: Identity with cookies (website) + bearer tokens (API / Postman) ----------
builder.Services
    .AddIdentityApiEndpoints<AppUser>(o =>
    {
        // relaxed rules for a demo app
        o.Password.RequiredLength = 6;
        o.Password.RequireNonAlphanumeric = false;
        o.Password.RequireUppercase = false;
        o.Password.RequireDigit = false;
        o.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddAuthentication(o =>
{
    // Unauthenticated website visitors are redirected to the login page (cookie scheme).
    o.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    o.DefaultForbidScheme = IdentityConstants.ApplicationScheme;
});

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/AccessDenied";
    o.ExpireTimeSpan = TimeSpan.FromDays(7);
    // API calls get 401/403 status codes instead of an HTML redirect
    o.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        else ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
    o.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        else ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization();

// ---------- MVC + API + SignalR + Swagger ----------
builder.Services.AddControllersWithViews(o =>
        // only validate what is explicitly marked [Required]
        o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSignalR();
builder.Services.AddScoped<CatalogNotifier>();
builder.Services.AddScoped<PosterStorage>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Movies API",
        Version = "v1",
        Description = "REST API of the movie catalogue. Log in with POST /api/auth/login, then click Authorize and paste the accessToken."
    });
    // only our API, and from the Identity endpoints only login + register
    c.DocInclusionPredicate((_, api) =>
        api.RelativePath?.StartsWith("api/") == true &&
        (!api.RelativePath.StartsWith("api/auth/") || AuthEndpoints.IsAllowed("/" + api.RelativePath)));
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        In = ParameterLocation.Header,
        Description = "Paste the accessToken returned by /api/auth/login"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
    });
});

var app = builder.Build();

await DbSeeder.SeedAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

// MapIdentityApi adds many endpoints (refresh, forgotPassword, 2FA, ...). We only use login + register,
// so every other /api/auth/... URL answers 404.
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api/auth") && !AuthEndpoints.IsAllowed(ctx.Request.Path))
    {
        ctx.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next();
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Movies API v1");
    c.DocumentTitle = "Movies API";
});

// Identity token endpoints: POST /api/auth/register and POST /api/auth/login (the rest is blocked above)
app.MapGroup("/api/auth").WithTags("Auth").MapIdentityApi<AppUser>();

app.MapHub<CatalogHub>("/hubs/catalog");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static class AuthEndpoints
{
    static readonly string[] Allowed = ["/api/auth/login", "/api/auth/register"];

    public static bool IsAllowed(string? path) =>
        Allowed.Any(a => string.Equals(path?.TrimEnd('/'), a, StringComparison.OrdinalIgnoreCase));
}
