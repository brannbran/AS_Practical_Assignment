using AS_Practical_Assignment.Models;
using AS_Practical_Assignment.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AS_Practical_Assignment.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<Member> _signInManager;
    private readonly UserManager<Member> _userManager;
        private readonly ISessionService _sessionService;
    private readonly IAuditService _auditService;
   private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(
      SignInManager<Member> signInManager,
   UserManager<Member> userManager,
       ISessionService sessionService,
      IAuditService auditService,
         ILogger<LogoutModel> logger)
        {
     _signInManager = signInManager;
 _userManager = userManager;
       _sessionService = sessionService;
 _auditService = auditService;
  _logger = logger;
 }

    public async Task<IActionResult> OnPostAsync()
  {
            // Get current user before signing out
       var user = await _userManager.GetUserAsync(User);

            if (user != null)
        {
    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
       var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

    // Audit: User logout
       await _auditService.LogLogoutAsync(user.Id, user.Email ?? "", ipAddress, userAgent);

    // Invalidate session
    await _sessionService.InvalidateSessionAsync(user);
    
    _logger.LogInformation($"User {user.Email} logged out and session invalidated.");
    }

          // Clear HTTP session
  HttpContext.Session.Clear();

 // Sign out (clears authentication cookies)
  await _signInManager.SignOutAsync();

   // Redirect to login page after logout
  return RedirectToPage("/Login");
      }

        public IActionResult OnGet()
{
  // Prevent GET requests, redirect to home
      return RedirectToPage("/Index");
        }
    }
}
