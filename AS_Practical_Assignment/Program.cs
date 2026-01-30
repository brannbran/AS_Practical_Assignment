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

// Configure Email Settings
builder.Services.Configure<EmailSettings>(
  builder.Configuration.GetSection("EmailSettings"));

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
    // Note: Session cookies are unique per browser tab by default in ASP.NET Core
    // Each tab that navigates to the login page gets a new session
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

// Add Password Policy Service
builder.Services.AddScoped<IPasswordPolicyService, PasswordPolicyService>();

// Add Password Reset Service
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();

// Add Two-Factor Authentication Service
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();

// Add Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

// Add Email OTP Service
builder.Services.AddScoped<IEmailOtpService, EmailOtpService>();

// Add Custom Password Hasher with additional salting
builder.Services.AddScoped<ICustomPasswordHasher, CustomPasswordHasher>();

// Replace default UserManager with CustomUserManager that uses enhanced password hashing
builder.Services.AddScoped<UserManager<Member>, CustomUserManager>();

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
    options.AccessDeniedPath = "/Error"; // For authenticated users without permission
    options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    
    // Custom handlers for authentication redirects
    options.Events = new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents
    {
  // Handle unauthenticated access (401 ? 403 for better UX)
        OnRedirectToLogin = context =>
        {
          // Check if this is an API call or AJAX request
 if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
    {
       context.Response.StatusCode = 401;
        return Task.CompletedTask;
}
            
            // For regular page requests, show 403 Forbidden instead of redirecting to login
            // This provides a better user experience with a clear error message
            context.Response.StatusCode = 403;
            context.Response.Redirect($"/Error?statusCode=403");
            return Task.CompletedTask;
        },
   
        // Handle access denied for authenticated users (already authenticated but no permission)
        OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = 403;
            context.Response.Redirect($"/Error?statusCode=403");
            return Task.CompletedTask;
        }
    };
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
    // In development, also use custom error pages (not developer exception page for better UX)
  app.UseExceptionHandler("/Error");
}

// Handle status code pages (401, 403, 404, 500, 503, etc.)
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
