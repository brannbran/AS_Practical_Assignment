using AS_Practical_Assignment.Models;
using AS_Practical_Assignment.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace AS_Practical_Assignment.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<Member> _signInManager;
        private readonly UserManager<Member> _userManager;
        private readonly ISessionService _sessionService;
        private readonly IAuditService _auditService;
        private readonly IReCaptchaService _reCaptchaService;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
         SignInManager<Member> signInManager,
    UserManager<Member> userManager,
            ISessionService sessionService,
         IAuditService auditService,
  IReCaptchaService reCaptchaService,
            ILogger<LoginModel> logger)
        {
        _signInManager = signInManager;
          _userManager = userManager;
      _sessionService = sessionService;
  _auditService = auditService;
          _reCaptchaService = reCaptchaService;
    _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string? ReturnUrl { get; set; }
        public string ReCaptchaSiteKey { get; set; } = string.Empty;

        [TempData]
    public string? ErrorMessage { get; set; }

  [TempData]
        public string? SessionExpiredMessage { get; set; }

  [TempData]
        public string? MultipleLoginMessage { get; set; }

 [TempData]
        public string? SuccessMessage { get; set; }

public class InputModel
        {
            [Required]
   [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

       [Required]
    [DataType(DataType.Password)]
        [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

   [Display(Name = "Remember me?")]
  public bool RememberMe { get; set; }
        }

        public void OnGet(string? returnUrl = null, bool sessionExpired = false, bool multipleLogin = false)
        {
   // Get reCAPTCHA site key
  ReCaptchaSiteKey = _reCaptchaService.GetSiteKey();

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
        ModelState.AddModelError(string.Empty, ErrorMessage);
    }

   if (sessionExpired)
            {
        SessionExpiredMessage = "Your session has expired due to inactivity. Please login again.";
}

            if (multipleLogin)
            {
          MultipleLoginMessage = "You have been logged out because you logged in from another location.";
            }

            ReturnUrl = returnUrl;
   }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

    // Validate reCAPTCHA
        var reCaptchaToken = Request.Form["g-recaptcha-response"].ToString();
     var reCaptchaResult = await _reCaptchaService.ValidateAsync(reCaptchaToken, "login", ipAddress);

            if (!reCaptchaResult.IsValid)
 {
    _logger.LogWarning($"reCAPTCHA validation failed for login attempt: {Input.Email}. Score: {reCaptchaResult.Score}");

          // Audit: reCAPTCHA failed
        await _auditService.LogAsync(
                Input.Email,
   Input.Email,
"reCAPTCHA Failed",
    AuditStatus.Failed,
       $"Bot detection failed during login. {reCaptchaResult.Message}",
      ipAddress,
    userAgent,
         $"Score: {reCaptchaResult.Score}"
  );

                ModelState.AddModelError(string.Empty, 
     "Bot detection failed. Please try again. If you believe this is an error, contact support.");
       ReCaptchaSiteKey = _reCaptchaService.GetSiteKey();
        return Page();
 }

if (ModelState.IsValid)
            {
                // Find user by email
     var user = await _userManager.FindByEmailAsync(Input.Email);

       if (user == null)
         {
          // Audit: Failed login - user not found
        await _auditService.LogLoginAttemptAsync(
      Input.Email,
               success: false,
       ipAddress,
   userAgent,
               reason: "User not found"
          );

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
      ReCaptchaSiteKey = _reCaptchaService.GetSiteKey();
          return Page();
             }

       // Check if account is locked out
        if (await _userManager.IsLockedOutAsync(user))
             {
   var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
          var remainingTime = lockoutEnd?.Subtract(DateTimeOffset.UtcNow);

   // Audit: Account locked
                  await _auditService.LogAsync(
     user.Id,
    user.Email ?? "",
                  AuditActions.AccountLocked,
                 AuditStatus.Warning,
             $"Account locked - attempted login while locked",
 ipAddress,
            userAgent
    );

           _logger.LogWarning($"User account {Input.Email} is locked out.");
                    ModelState.AddModelError(string.Empty,
       $"Account is locked due to multiple failed login attempts. Please try again in {remainingTime?.Minutes ?? 15} minutes.");
     ReCaptchaSiteKey = _reCaptchaService.GetSiteKey();
     return Page();
       }

         // Check for existing active session (multiple login detection)
    var newSessionId = Guid.NewGuid().ToString();
    var hasMultipleLogin = await _sessionService.DetectMultipleLoginsAsync(user, newSessionId);

            if (hasMultipleLogin)
      {
          _logger.LogWarning($"Multiple login detected for user {Input.Email}");

       // Audit: Multiple login detected
       await _auditService.LogMultipleLoginAsync(user.Id, user.Email ?? "", ipAddress, userAgent);

     // Invalidate old session
      await _sessionService.InvalidateSessionAsync(user);

  // Clear any existing sign-in
    await _signInManager.SignOutAsync();
           HttpContext.Session.Clear();
       }

                // Attempt to sign in with lockout enabled
  var result = await _signInManager.PasswordSignInAsync(
              Input.Email,
                  Input.Password,
   Input.RememberMe,
      lockoutOnFailure: true);

         if (result.Succeeded)
         {
         // Create secure session
  await _sessionService.CreateSecureSessionAsync(user, HttpContext);

          // Audit: Successful login
await _auditService.LogLoginAttemptAsync(
                  Input.Email,
          success: true,
               ipAddress,
       userAgent
           );

         _logger.LogInformation($"User {Input.Email} logged in successfully.");

        if (hasMultipleLogin)
       {
     TempData["MultipleLoginWarning"] = "You were logged out from your previous session.";
     }

             // Add a flag to indicate fresh login (for session monitor)
  TempData["FreshLogin"] = "true";

        // Redirect to homepage after successful login
         return LocalRedirect(returnUrl);
             }

 if (result.IsLockedOut)
      {
         // Audit: Account just got locked
               await _auditService.LogAsync(
    user.Id,
                user.Email ?? "",
          AuditActions.AccountLocked,
        AuditStatus.Warning,
         "Account locked after 3 failed login attempts",
                ipAddress,
      userAgent
         );

     _logger.LogWarning($"User account {Input.Email} locked out after failed attempts.");
  ModelState.AddModelError(string.Empty,
      "Account locked due to 3 failed login attempts. Please try again in 15 minutes.");
     ReCaptchaSiteKey = _reCaptchaService.GetSiteKey();
        return Page();
        }
                else
         {
   // Get failed access count
     var failedCount = await _userManager.GetAccessFailedCountAsync(user);

       // Audit: Failed login attempt
       await _auditService.LogLoginAttemptAsync(
             Input.Email,
         success: false,
           ipAddress,
     userAgent,
           reason: $"Invalid password (Attempt {failedCount}/3)"
           );

     var remainingAttempts = 3 - failedCount;
            ModelState.AddModelError(string.Empty,
                   $"Invalid login attempt. {remainingAttempts} attempt(s) remaining before account lockout.");
             ReCaptchaSiteKey = _reCaptchaService.GetSiteKey();
             return Page();
              }
       }

            ReCaptchaSiteKey = _reCaptchaService.GetSiteKey();
            return Page();
 }
    }
}
