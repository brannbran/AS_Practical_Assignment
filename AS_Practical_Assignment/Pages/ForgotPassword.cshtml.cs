using AS_Practical_Assignment.Models;
using AS_Practical_Assignment.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace AS_Practical_Assignment.Pages
{
    public class ForgotPasswordModel : PageModel
    {
private readonly UserManager<Member> _userManager;
 private readonly IPasswordResetService _passwordResetService;
      private readonly IAuditService _auditService;
  private readonly ILogger<ForgotPasswordModel> _logger;

   public ForgotPasswordModel(
    UserManager<Member> userManager,
  IPasswordResetService passwordResetService,
     IAuditService auditService,
    ILogger<ForgotPasswordModel> logger)
        {
 _userManager = userManager;
  _passwordResetService = passwordResetService;
   _auditService = auditService;
     _logger = logger;
  }

        [BindProperty]
 public InputModel Input { get; set; } = new InputModel();

 [TempData]
   public string? StatusMessage { get; set; }

    public class InputModel
 {
   [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
       [Display(Name = "Email Address")]
   public string Email { get; set; } = string.Empty;
        }

   public void OnGet()
    {
   }

        public async Task<IActionResult> OnPostAsync()
     {
 if (!ModelState.IsValid)
 {
    return Page();
   }

  var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
   var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

   var user = await _userManager.FindByEmailAsync(Input.Email);

   // Security: Always show success message even if user doesn't exist
// This prevents email enumeration attacks
            if (user == null)
   {
      _logger.LogWarning($"Password reset requested for non-existent email: {Input.Email}");
      
  // Audit attempt
  await _auditService.LogAsync(
   "",
       Input.Email,
 "Password Reset Request",
   AuditStatus.Failed,
"Reset requested for non-existent account",
   ipAddress,
    userAgent
      );

   // Show success message anyway
    StatusMessage = "? If an account with that email exists, a password reset link has been sent.";
       return Page();
  }

     // Generate reset token
  var token = await _passwordResetService.GeneratePasswordResetTokenAsync(user, ipAddress ?? "Unknown");

     // TODO: Send email with reset link
 // In production, you would send an email like:
 // var resetUrl = Url.Page("/ResetPassword", null, new { token }, Request.Scheme);
// await _emailService.SendPasswordResetEmailAsync(user.Email, resetUrl);

       // Do not log the password reset token or reset URL, as they are sensitive
  _logger.LogInformation("Password reset token generated for {Email}. Reset instructions have been sent if the email is registered.", user.Email);
            var sanitizedScheme = (Request.Scheme ?? string.Empty).Replace("\r", "").Replace("\n", "");
            var sanitizedHost = (Request.Host.ToString() ?? string.Empty).Replace("\r", "").Replace("\n", "");
            _logger.LogInformation("Password reset URL generated for {Email}.", user.Email);


            // Audit successful request
            await _auditService.LogAsync(
   user.Id,
  user.Email ?? "",
    "Password Reset Request",
  AuditStatus.Success,
        "Password reset token generated",
  ipAddress,
       userAgent    
   );

   StatusMessage = "? If an account with that email exists, a password reset link has been sent. Please check your email.";
       
       return Page();
}
    }
}
