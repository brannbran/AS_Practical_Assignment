using AS_Practical_Assignment.Data;
using AS_Practical_Assignment.Middleware;
using AS_Practical_Assignment.Models;
using AS_Practical_Assignment.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Configure Google reCAPTCHA
builder.Services.Configure<GoogleReCaptchaConfig>(
  builder.Configuration.GetSection("GoogleReCaptcha"));

// Add HttpClient for reCAPTCHA service
builder.Services.AddHttpClient();

// Add Session support with timeout
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15); // 15 minute timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Data Protection for encryption
builder.Services.AddDataProtection();

// Add Encryption Service
builder.Services.AddScoped<IEncryptionService, EncryptionService>();

// Add Session Service
builder.Services.AddScoped<ISessionService, SessionService>();

// Add Audit Service
builder.Services.AddScoped<IAuditService, AuditService>();

// Add reCAPTCHA Service
builder.Services.AddScoped<IReCaptchaService, ReCaptchaService>();

// Add Identity with STRONG password requirements and account lockout
builder.Services.AddIdentity<Member, IdentityRole>(options =>
{
    // STRONG Password settings - Min 12 characters
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;
    options.Password.RequiredUniqueChars = 4;

    // User settings
    options.User.RequireUniqueEmail = true;

    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = false;
    
    // Lockout settings (Rate Limiting) - Account lockout after 3 failed attempts
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 3; // CHANGED TO 3 for rate limiting
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure application cookie with security settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.AccessDeniedPath = "/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    // In development, show detailed error page
    app.UseDeveloperExceptionPage();
}

// Handle status code pages (404, 403, etc.)
app.UseStatusCodePagesWithReExecute("/Error", "?statusCode={0}");

app.UseHttpsRedirection();

app.UseRouting();

// Enable session before authentication
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Add session validation middleware (after authentication)
app.UseSessionValidation();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
