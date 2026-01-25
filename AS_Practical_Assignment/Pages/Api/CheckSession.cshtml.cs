using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using AS_Practical_Assignment.Services;
using Microsoft.AspNetCore.Identity;
using AS_Practical_Assignment.Models;
using Microsoft.Extensions.Logging;

namespace AS_Practical_Assignment.Pages.Api
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class CheckSessionModel : PageModel
    {
      private readonly UserManager<Member> _userManager;
        private readonly ISessionService _sessionService;
  private readonly ILogger<CheckSessionModel> _logger;

        public CheckSessionModel(
  UserManager<Member> userManager, 
   ISessionService sessionService,
       ILogger<CheckSessionModel> logger)
  {
  _userManager = userManager;
       _sessionService = sessionService;
 _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
  {
 try
    {
   var user = await _userManager.GetUserAsync(User);
     if (user == null)
           {
     _logger.LogWarning("CheckSession: User not found");
        return new JsonResult(new { isValid = false, reason = "user_not_found" });
           }

      // Reload user from database to get latest session info
 user = await _userManager.FindByIdAsync(user.Id);

    var isValid = await _sessionService.ValidateSessionAsync(user, HttpContext);

  if (!isValid)
        {
  _logger.LogWarning($"CheckSession: Session invalid for user {user.Email}");
   return new JsonResult(new { isValid = false, reason = "session_invalidated" });
       }

 _logger.LogDebug($"CheckSession: Session valid for user {user.Email}");
        return new JsonResult(new { isValid = true });
  }
            catch (Exception ex)
        {
    _logger.LogError(ex, "CheckSession: Error occurred");
       return new JsonResult(new { isValid = false, reason = "error" });
      }
        }
 }
}
