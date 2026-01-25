using AS_Practical_Assignment.Models;
using AS_Practical_Assignment.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AS_Practical_Assignment.Pages
{
    [Authorize]
    public class ActivityLogModel : PageModel
    {
  private readonly UserManager<Member> _userManager;
 private readonly IAuditService _auditService;

      public ActivityLogModel(UserManager<Member> userManager, IAuditService auditService)
        {
  _userManager = userManager;
  _auditService = auditService;
      }

        public List<AuditLog> UserActivities { get; set; } = new List<AuditLog>();
        public string? UserEmail { get; set; }

    public async Task OnGetAsync()
        {
  var user = await _userManager.GetUserAsync(User);
      if (user != null)
 {
       UserEmail = user.Email;
   UserActivities = await _auditService.GetUserAuditLogsAsync(user.Id, count: 20);
        }
 }
    }
}
