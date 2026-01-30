using AS_Practical_Assignment.Models;
using AS_Practical_Assignment.Services;
using AS_Practical_Assignment.Validators;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace AS_Practical_Assignment.Pages
{
    public class ResetPasswordModel : PageModel
    {
   private readonly UserManager<Member> _userManager;
    private readonly IPasswordResetService _passwordResetService;
  private readonly IPasswordPolicyService _passwordPolicyService;
  private readonly IAuditService _auditService;
 private readonly ILogger<ResetPasswordModel> _logger;

 public ResetPasswordModel(
     UserManager<Member> userManager,
   IPasswordResetService passwordResetService,
 IPasswordPolicyService passwordPolicyService,
   IAuditService auditService,
    ILogger<ResetPasswordModel> logger)
 {
   _userManager = userManager;
_passwordResetService = passwordResetService;
_passwordPolicyService = passwordPolicyService;
    _auditService = auditService;
 _logger = logger;
        }

  [BindProperty]
 public InputModel Input { get; set; } = new InputModel();

     [BindProperty(SupportsGet = true)]
 public string? Token { get; set; }

 public bool IsValidToken { get; set; }

     [TempData]
   public string? StatusMessage { get; set; }

public class InputModel
        {
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
  if (string.IsNullOrEmpty(Token))
 {
 StatusMessage = "? Invalid password reset link.";
 IsValidToken = false;
    return Page();
 }

// Validate token
    var (isValid, member) = await _passwordResetService.ValidateResetTokenAsync(Token);
   
    IsValidToken = isValid;
            
    if (!isValid)
  {
   StatusMessage = "? This password reset link is invalid or has expired. Please request a new one.";
  }

    return Page();
    }

        public async Task<IActionResult> OnPostAsync()
 {
     if (string.IsNullOrEmpty(Token))
    {
        StatusMessage = "? Invalid password reset link.";
 IsValidToken = false;
 return Page();
  }

  if (!ModelState.IsValid)
   {
     IsValidToken = true;
return Page();
 }

  var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
 var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

  // Validate token and get user
      var (isValid, member) = await _passwordResetService.ValidateResetTokenAsync(Token);
   
     if (!isValid || member == null)
            {
   StatusMessage = "? This password reset link is invalid or has expired. Please request a new one.";
      IsValidToken = false;
       
    // Audit failed attempt
  await _auditService.LogAsync(
         "",
   "",
 "Password Reset Failed",
      AuditStatus.Failed,
      "Invalid or expired token",
   ipAddress,
 userAgent
        );

      return Page();
     }

   // IMPORTANT: Ensure user is NOT logged in during password reset
            // This prevents security issues where someone might abuse the reset flow
if (User.Identity?.IsAuthenticated == true)
            {
  _logger.LogWarning($"User attempted password reset while logged in: {member.Email}");
      
       // Force logout if user is logged in
       HttpContext.Session.Clear();
       Response.Cookies.Delete(".AspNetCore.Identity.Application");
       
         ModelState.AddModelError(string.Empty, "For security reasons, please log out before resetting your password.");
 IsValidToken = false;
   return RedirectToPage("/Logout", new { returnUrl = $"/ResetPassword?token={Token}" });
 }

    // Check password policy (password reuse)
      var isPasswordReused = await _passwordPolicyService.IsPasswordReusedAsync(member, Input.NewPassword);

       if (isPasswordReused)
   {
       _logger.LogWarning($"Password reset failed for {member.Email}: Password reused");
       
     ModelState.AddModelError(string.Empty, "You cannot reuse your last 2 passwords. Please choose a different password.");
        IsValidToken = true;
                
  // Audit policy violation
         await _auditService.LogAsync(
         member.Id,
    member.Email ?? "",
   "Password Reset Failed",
         AuditStatus.Failed,
     "Attempted to reuse old password",
       ipAddress,
        userAgent
 );

    return Page();
      }

 // Reset password
  var resetToken = await _userManager.GeneratePasswordResetTokenAsync(member);
            var result = await _userManager.ResetPasswordAsync(member, resetToken, Input.NewPassword);

    if (!result.Succeeded)
       {
      _logger.LogError($"Password reset failed for {member.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
      
      foreach (var error in result.Errors)
{
       ModelState.AddModelError(string.Empty, error.Description);
 }

   IsValidToken = true;
  
   // Audit failed attempt
   await _auditService.LogAsync(
   member.Id,
        member.Email ?? "",
    "Password Reset Failed",
  AuditStatus.Failed,
   "Password reset failed: " + string.Join(", ", result.Errors.Select(e => e.Description)),
    ipAddress,
        userAgent
);

   return Page();
       }

  // Add password to history
   await _passwordPolicyService.AddPasswordToHistoryAsync(member, member.PasswordHash!);

 // Set password expiry
 await _passwordPolicyService.SetPasswordExpiryAsync(member);

 // Mark token as used
   await _passwordResetService.MarkTokenAsUsedAsync(Token);

  // Audit successful reset
       await _auditService.LogAsync(
  member.Id,
    member.Email ?? "",
 "Password Reset",
   AuditStatus.Success,
    "Password reset successfully via email link",
        ipAddress,
   userAgent
 );

  _logger.LogInformation($"User {member.Email} reset their password successfully.");

     TempData["StatusMessage"] = "? Your password has been reset successfully. You can now login with your new password.";

  return RedirectToPage("/Login");
        }
    }
}
