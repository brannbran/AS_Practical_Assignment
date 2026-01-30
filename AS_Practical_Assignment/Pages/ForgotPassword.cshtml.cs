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
        private readonly IEmailService _emailService;
  private readonly IAuditService _auditService;
      private readonly ILogger<ForgotPasswordModel> _logger;

    public ForgotPasswordModel(
  UserManager<Member> userManager,
   IPasswordResetService passwordResetService,
            IEmailService emailService,
            IAuditService auditService,
       ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _passwordResetService = passwordResetService;
            _emailService = emailService;
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

     // Show success message anyway (security best practice)
     StatusMessage = "? If an account with that email exists, a password reset link has been sent. Please check your email.";
     return Page();
       }

            // Generate reset token
        var token = await _passwordResetService.GeneratePasswordResetTokenAsync(user, ipAddress ?? "Unknown");

     // Generate reset URL
        var resetUrl = Url.Page(
       "/ResetPassword",
   pageHandler: null,
    values: new { token },
      protocol: Request.Scheme);

        if (string.IsNullOrEmpty(resetUrl))
     {
              _logger.LogError($"Failed to generate reset URL for {Input.Email}");
  StatusMessage = "? An error occurred. Please try again.";
      return Page();
            }

            // Send password reset email
            var emailSent = await _emailService.SendPasswordResetEmailAsync(user.Email!, resetUrl);

 if (!emailSent)
  {
         _logger.LogError($"Failed to send password reset email to {Input.Email}");
           
       // Audit failed email send
    await _auditService.LogAsync(
     user.Id,
    user.Email ?? "",
             "Password Reset Email Failed",
           AuditStatus.Failed,
        "Failed to send password reset email",
      ipAddress,
     userAgent
                );

       StatusMessage = "? Failed to send reset email. Please try again.";
          return Page();
      }

  _logger.LogInformation($"Password reset email sent successfully to {Input.Email}");

    // Audit successful request
            await _auditService.LogAsync(
                user.Id,
            user.Email ?? "",
            "Password Reset Requested",
  AuditStatus.Success,
    "Password reset email sent",
     ipAddress,
                userAgent
            );

            StatusMessage = "? If an account with that email exists, a password reset link has been sent. Please check your email.";
   
            return Page();
        }
    }
}
