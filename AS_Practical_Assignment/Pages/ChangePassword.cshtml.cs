using AS_Practical_Assignment.Models;
using AS_Practical_Assignment.Services;
using AS_Practical_Assignment.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace AS_Practical_Assignment.Pages
{
 [Authorize]
    public class ChangePasswordModel : PageModel
    {
     private readonly UserManager<Member> _userManager;
  private readonly SignInManager<Member> _signInManager;
private readonly IPasswordPolicyService _passwordPolicyService;
        private readonly IAuditService _auditService;
  private readonly ILogger<ChangePasswordModel> _logger;

     public ChangePasswordModel(
     UserManager<Member> userManager,
   SignInManager<Member> signInManager,
     IPasswordPolicyService passwordPolicyService,
   IAuditService auditService,
       ILogger<ChangePasswordModel> logger)
   {
   _userManager = userManager;
   _signInManager = signInManager;
     _passwordPolicyService = passwordPolicyService;
      _auditService = auditService;
   _logger = logger;
  }

 [BindProperty]
public InputModel Input { get; set; } = new InputModel();

        [TempData]
        public string? StatusMessage { get; set; }

     public bool IsPasswordExpired { get; set; }

        public class InputModel
        {
 [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
   [Display(Name = "Current Password")]
  public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
   [StrongPassword]
       [DataType(DataType.Password)]
   [Display(Name = "New Password")]
     public string NewPassword { get; set; } = string.Empty;

       [Required(ErrorMessage = "Please confirm your new password")]
    [DataType(DataType.Password)]
     [Display(Name = "Confirm New Password")]
     [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
       public string ConfirmPassword { get; set; } = string.Empty;
        }

 public async Task<IActionResult> OnGetAsync()
   {
var user = await _userManager.GetUserAsync(User);
   if (user == null)
     {
       return NotFound("Unable to load user.");
   }

    // Check if password is expired
     IsPasswordExpired = await _passwordPolicyService.IsPasswordExpiredAsync(user);

       if (IsPasswordExpired)
{
       StatusMessage = "?? Your password has expired. You must change your password to continue.";
 }

   return Page();
 }

    public async Task<IActionResult> OnPostAsync()
 {
      if (!ModelState.IsValid)
       {
       return Page();
  }

       var user = await _userManager.GetUserAsync(User);
  if (user == null)
   {
     return NotFound("Unable to load user.");
       }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
 var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

       // Verify current password
      var isCurrentPasswordCorrect = await _userManager.CheckPasswordAsync(user, Input.CurrentPassword);
   if (!isCurrentPasswordCorrect)
  {
          ModelState.AddModelError(string.Empty, "Current password is incorrect.");
    
   // Audit failed attempt
   await _auditService.LogAsync(
     user.Id,
      user.Email ?? "",
    "Password Change Failed",
    AuditStatus.Failed,
      "Incorrect current password",
  ipAddress,
     userAgent
 );

       return Page();
      }

  // Validate password policy
 var policyValidation = await _passwordPolicyService.ValidatePasswordPolicyAsync(user, Input.NewPassword);
  if (!policyValidation.success)
   {
ModelState.AddModelError(string.Empty, policyValidation.error!);
   
 // Audit policy violation
   await _auditService.LogAsync(
     user.Id,
  user.Email ?? "",
       "Password Change Failed",
 AuditStatus.Failed,
     $"Policy violation: {policyValidation.error}",
      ipAddress,
  userAgent
      );

    return Page();
 }

// Change password
       var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
   if (!changePasswordResult.Succeeded)
      {
      foreach (var error in changePasswordResult.Errors)
      {
    ModelState.AddModelError(string.Empty, error.Description);
  }

    // Audit failed attempt
    await _auditService.LogAsync(
 user.Id,
  user.Email ?? "",
       "Password Change Failed",
AuditStatus.Failed,
"Password change failed: " + string.Join(", ", changePasswordResult.Errors.Select(e => e.Description)),
   ipAddress,
    userAgent
 );

      return Page();
      }

  // Add password to history
 await _passwordPolicyService.AddPasswordToHistoryAsync(user, user.PasswordHash!);

  // Set password expiry
 await _passwordPolicyService.SetPasswordExpiryAsync(user);

  // Audit successful change
  await _auditService.LogAsync(
   user.Id,
    user.Email ?? "",
  "Password Changed",
    AuditStatus.Success,
   "Password changed successfully",
   ipAddress,
      userAgent
     );

         _logger.LogInformation($"User {user.Email} changed their password successfully.");

      // Sign in again to refresh the security stamp
  await _signInManager.RefreshSignInAsync(user);

 StatusMessage = "? Your password has been changed successfully.";

  return RedirectToPage();
}
    }
}
