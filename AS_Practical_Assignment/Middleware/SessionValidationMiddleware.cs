using AS_Practical_Assignment.Models;
using AS_Practical_Assignment.Services;
using Microsoft.AspNetCore.Identity;

namespace AS_Practical_Assignment.Middleware
{
    public class SessionValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SessionValidationMiddleware> _logger;

        public SessionValidationMiddleware(RequestDelegate next, ILogger<SessionValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<Member> userManager, 
        ISessionService sessionService, SignInManager<Member> signInManager, IAuditService auditService)
        {
            // Skip validation for login, logout, public pages, and API endpoints
            var path = context.Request.Path.Value?.ToLower() ?? "";
            if (path.Contains("/login") || path.Contains("/logout") || path.Contains("/register") ||
                path.Contains("/verifyemail") || path.Contains("/forgotpassword") || path.Contains("/resetpassword") ||
                path.Contains("/lib/") || path.Contains("/css/") || path.Contains("/js/") ||
                path.Contains("/api/") ||  // Skip API endpoints
                !context.User.Identity?.IsAuthenticated == true)
            {
                await _next(context);
                return;
            }

            // Ensure session is loaded before validation
            await context.Session.LoadAsync();

            // Check if this is a fresh login (skip validation for first few requests)
            var justLoggedIn = context.Session.GetString("JustLoggedIn");
            if (!string.IsNullOrEmpty(justLoggedIn))
            {
                _logger.LogDebug("Fresh login detected - skipping session validation for this request");
                
                // Clear the flag after first request
                context.Session.Remove("JustLoggedIn");
                await context.Session.CommitAsync();
                
                // Allow the request to proceed without validation
                await _next(context);
                return;
            }

            // Get current user
            var user = await userManager.GetUserAsync(context.User);
            if (user != null)
            {
                // Reload user from database to ensure we have latest session info
                user = await userManager.FindByIdAsync(user.Id);
        
                if (user == null)
                {
                    _logger.LogWarning("User not found after FindByIdAsync");
                    await _next(context);
                    return;
                }

                // Validate session
                var isValid = await sessionService.ValidateSessionAsync(user, context);

                if (!isValid)
                {
                    // Session invalid - log out and redirect
                    _logger.LogWarning($"Invalid session detected for user {user.Email}. Logging out.");

                    // Audit: Session expired
                    await auditService.LogSessionExpiredAsync(user.Id, user.Email ?? "");

                    // IMPORTANT: Sign out FIRST to clear authentication cookie
                    await signInManager.SignOutAsync();
                    
                    // Clear HTTP session
                    context.Session.Clear();
                    
                    // DON'T invalidate database session here - it belongs to the other browser!
                    // The database session is still valid for the browser that logged in most recently.
                    // We only clear THIS browser's cookies and session.

                    // Redirect to login with timeout message
                    context.Response.Redirect("/Login?sessionExpired=true");
                    return;
                }

                // Update last activity
                await sessionService.UpdateLastActivityAsync(user);
            }

            await _next(context);
        }
    }

    public static class SessionValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseSessionValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SessionValidationMiddleware>();
        }
    }
}
