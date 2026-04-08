using InternshipManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using InternshipManagementSystem.Helpers;
using Microsoft.AspNetCore.Http;
using InternshipManagementSystem.Services;
using Microsoft.AspNetCore.HttpOverrides;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Step 4: Dynamic Port Binding (Render/Railway inject $PORT; Docker uses ASPNETCORE_URLS)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}
// If PORT is not set, ASPNETCORE_URLS env var (set in Dockerfile to 8080) takes effect automatically


// Step 7: Logging (Enable default ASP.NET logging)
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

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

// Step 6: Enable HTTPS redirection safe for proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IGuideService, GuideService>();
builder.Services.AddScoped<IDailyDiaryService, DailyDiaryService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

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
              .AllowCredentials(); // Needed if relying on existing Cookies/Auth
    });
});

var app = builder.Build();

// Step 3: Ensure Upload Directory Exists
var webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var uploadsPath = Path.Combine(webRoot, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// Step 2 & 7: Database Initialization and Logging
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try 
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        if (app.Environment.IsProduction())
        {
            // SQLite: Use EnsureCreated (avoids SQL Server-specific migration scripts)
            // This creates the schema directly from the EF model.
            logger.LogInformation("Production environment: ensuring SQLite database is created...");
            context.Database.EnsureCreated();
            logger.LogInformation("SQLite database ready.");
        }
        else
        {
            // SQL Server: Apply pending migrations
            logger.LogInformation("Development environment: applying pending SQL Server migrations...");
            context.Database.Migrate();
            logger.LogInformation("Migrations applied successfully.");
        }
        
        DbSeeder.Initialize(services);
        logger.LogInformation("Database seeding complete.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<ApiLoggingMiddleware>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Step 6: Forward headers (must be early in pipeline)
app.UseForwardedHeaders();

// Only redirect HTTPS in development; in production Render/Railway handle TLS at the proxy level
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Step 5: Production Middleware Ordering
app.UseStaticFiles();
app.UseRouting();
app.UseCors("ReactPolicy");
app.UseAuthentication();
app.UseAuthorization();

// Middleware to ensure 'sid' is present in URL
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.Trim('/');
    
    // Skip static files, API calls, and segments that already look like 'sid'
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

    // If first segment is not a SID (sXXXX), redirect to a new one
    if (firstSegment.Length < 2 || !firstSegment.StartsWith("s") || !char.IsLetterOrDigit(firstSegment[1]))
    {
        // Don't redirect images, js, css, etc.
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

// Note: /api/health is handled by HealthController — no duplicate needed here

app.Run();
