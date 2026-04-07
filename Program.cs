using InternshipManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using InternshipManagementSystem.Helpers;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.CookieManager = new MultiSessionCookieManager();
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    DbSeeder.Initialize(services);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

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

app.Run();
