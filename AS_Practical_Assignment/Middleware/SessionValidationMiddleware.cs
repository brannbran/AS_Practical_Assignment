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
                path.Contains("/lib/") || path.Contains("/css/") || path.Contains("/js/") ||
                path.Contains("/api/") ||  // Skip API endpoints
                !context.User.Identity?.IsAuthenticated == true)
            {
                await _next(context);
                return;
            }

            // Get current user
            var user = await userManager.GetUserAsync(context.User);
            if (user != null)
            {
                // Validate session
                var isValid = await sessionService.ValidateSessionAsync(user, context);

                if (!isValid)
                {
                    // Session invalid - log out and redirect
                    _logger.LogWarning($"Invalid session detected for user {user.Email}. Logging out.");

                    // Audit: Session expired
                    await auditService.LogSessionExpiredAsync(user.Id, user.Email ?? "");

                    await signInManager.SignOutAsync();
                    await sessionService.InvalidateSessionAsync(user);
                    context.Session.Clear();

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
