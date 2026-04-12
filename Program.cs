using InternshipManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using InternshipManagementSystem.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Step 1: Dynamic Port Binding (Render/Railway inject $PORT; Docker uses ASPNETCORE_URLS)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Step 2: Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Step 3: Persist DataProtection Keys
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/var/data/keys"))
    .SetApplicationName("InternshipManagementSystem");

if (builder.Environment.IsProduction())
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.CookieManager = new MultiSessionCookieManager();
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Step 4: Ensure Upload Directory Exists
var webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var uploadsPath = Path.Combine(webRoot, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// Step 5: Database Initialization
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try 
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        if (app.Environment.IsProduction())
        {
            logger.LogInformation("Production: ensuring SQLite database is created...");
            context.Database.EnsureCreated();
        }
        else
        {
            logger.LogInformation("Development: applying migrations...");
            context.Database.Migrate();
        }
        
        DbSeeder.Initialize(services);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

app.UseMiddleware<GlobalExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                      ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors("ReactPolicy");
app.UseAuthentication();
app.UseAuthorization();

// Middleware to ensure 'sid' is present in URL
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.Trim('/');
    
    if (string.IsNullOrEmpty(path) || path.Equals("Account/Login", StringComparison.OrdinalIgnoreCase))
    {
        var sid = "s" + Guid.NewGuid().ToString("N").Substring(0, 5);
        context.Response.Redirect($"/{sid}/Account/Login");
        return;
    }

    if (path.Equals("Account/Logout", StringComparison.OrdinalIgnoreCase))
    {
        var sid = "s" + Guid.NewGuid().ToString("N").Substring(0, 5);
        context.Response.Redirect($"/{sid}/Account/Login");
        return;
    }

    var segments = path.Split('/');
    var firstSegment = segments[0];

    if (firstSegment.Length < 2 || !firstSegment.StartsWith("s") || !char.IsLetterOrDigit(firstSegment[1]))
    {
        if (!path.Contains(".") && !path.StartsWith("lib"))
        {
            var sid = "s" + Guid.NewGuid().ToString("N").Substring(0, 5);
            context.Response.Redirect($"/{sid}/{path}{context.Request.QueryString}");
            return;
        }
    }

    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{sid}/{controller=Account}/{action=Login}/{id?}");

if (!OperatingSystem.IsWindows())
    Directory.CreateDirectory("/var/data/keys");

app.Run();
