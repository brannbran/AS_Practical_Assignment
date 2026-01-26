using AS_Practical_Assignment.Models;
using AS_Practical_Assignment.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.IO;

namespace AS_Practical_Assignment.Pages
{
    public class VerifyEmailModel : PageModel
    {
        private readonly UserManager<Member> _userManager;
        private readonly SignInManager<Member> _signInManager;
        private readonly IEmailOtpService _emailOtpService;
        private readonly IEmailService _emailService;
private readonly IEncryptionService _encryptionService;
        private readonly ISessionService _sessionService;
    private readonly IAuditService _auditService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<VerifyEmailModel> _logger;

        public VerifyEmailModel(
    UserManager<Member> userManager,
  SignInManager<Member> signInManager,
   IEmailOtpService emailOtpService,
  IEmailService emailService,
    IEncryptionService encryptionService,
   ISessionService sessionService,
   IAuditService auditService,
       IWebHostEnvironment environment,
   ILogger<VerifyEmailModel> logger)
        {
            _userManager = userManager;
     _signInManager = signInManager;
       _emailOtpService = emailOtpService;
      _emailService = emailService;
_encryptionService = encryptionService;
 _sessionService = sessionService;
   _auditService = auditService;
       _environment = environment;
_logger = logger;
  }

        [BindProperty(SupportsGet = true)]
    public string Email { get; set; } = string.Empty;

        [BindProperty]
    public InputModel Input { get; set; } = new InputModel();

  public string? ErrorMessage { get; set; }

        public class InputModel
  {
   [Required(ErrorMessage = "Verification code is required")]
     [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 digits")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Verification code must be 6 digits")]
    [Display(Name = "Verification Code")]
  public string OtpCode { get; set; } = string.Empty;
 }

   public IActionResult OnGet()
        {
if (string.IsNullOrEmpty(Email))
        {
  return RedirectToPage("/Register");
   }

          return Page();
}

  public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
{
     return Page();
      }

       var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
 var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

       // Validate OTP
       var (isValid, otpToken) = await _emailOtpService.ValidateOtpAsync(Email, Input.OtpCode);

          if (!isValid || otpToken == null)
            {
       _logger.LogWarning($"Invalid OTP attempt for {Email}");
   
   // Audit failed OTP
  await _auditService.LogAsync(
         Email,
        Email,
    "OTP Verification Failed",
           AuditStatus.Failed,
      "Invalid or expired OTP code",
     ipAddress,
        userAgent
                );

       ErrorMessage = "Invalid or expired verification code. Please try again or request a new code.";
       return Page();
    }

       // Deserialize registration data
            RegistrationTempData? regData;
     try
  {
                regData = JsonSerializer.Deserialize<RegistrationTempData>(otpToken.RegistrationDataJson!);
 if (regData == null)
 {
         throw new Exception("Failed to deserialize registration data");
    }
        }
    catch (Exception ex)
          {
      _logger.LogError(ex, $"Failed to deserialize registration data for {Email}");
    ErrorMessage = "An error occurred during verification. Please start registration again.";
return Page();
      }

   // Check if email already exists (double-check)
   var existingUser = await _userManager.FindByEmailAsync(Email);
      if (existingUser != null)
          {
   ErrorMessage = "This email address is already registered.";
     return Page();
  }

 // Handle resume file if provided
            string? resumePath = null;
if (regData.ResumeData != null && !string.IsNullOrEmpty(regData.ResumeFileName))
   {
      try
  {
      var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "resumes");
        Directory.CreateDirectory(uploadsFolder);

   var uniqueFileName = $"{Guid.NewGuid()}_{regData.ResumeFileName}";
   var filePath = Path.Combine(uploadsFolder, uniqueFileName);

  await System.IO.File.WriteAllBytesAsync(filePath, regData.ResumeData);
     resumePath = Path.Combine("uploads", "resumes", uniqueFileName);
   }
     catch (Exception ex)
    {
   _logger.LogError(ex, $"Failed to save resume for {Email}");
     // Continue with registration even if resume save fails
  }
   }

  // Create member with ENCRYPTED customer data
   var member = new Member
   {
   UserName = Email,
  Email = Email,
        EncryptedFirstName = _encryptionService.Encrypt(regData.FirstName),
    EncryptedLastName = _encryptionService.Encrypt(regData.LastName),
      Gender = regData.Gender,
  EncryptedNRIC = _encryptionService.Encrypt(regData.NRIC),
     EncryptedDateOfBirth = _encryptionService.Encrypt(regData.DateOfBirth.ToString("yyyy-MM-dd")),
    ResumePath = resumePath,
  EncryptedWhoAmI = string.IsNullOrEmpty(regData.WhoAmI) ? null : _encryptionService.Encrypt(regData.WhoAmI),
   CreatedDate = DateTime.UtcNow,
 EmailConfirmed = true // Email is verified via OTP
 };

   var result = await _userManager.CreateAsync(member, regData.Password);

     if (result.Succeeded)
            {
      _logger.LogInformation($"User {Email} created account successfully after OTP verification");

     // Mark OTP as used
    await _emailOtpService.MarkOtpAsUsedAsync(otpToken.Id);

       // Audit: User registration
          await _auditService.LogAsync(
         member.Id,
    member.Email ?? "",
     "OTP Verified & Registered",
AuditStatus.Success,
   "User verified email and completed registration",
  ipAddress,
      userAgent
     );

       // Send welcome email
 try
     {
 await _emailService.SendWelcomeEmailAsync(Email, regData.FirstName);
      }
      catch (Exception ex)
  {
         _logger.LogError(ex, $"Failed to send welcome email to {Email}");
     // Don't fail registration if welcome email fails
   }

   // Sign in the user
           await _signInManager.SignInAsync(member, isPersistent: false);

    // Create secure session
    await _sessionService.CreateSecureSessionAsync(member, HttpContext);

    // Audit: Auto-login after registration
      await _auditService.LogLoginAttemptAsync(
             member.Email ?? "",
         success: true,
       ipAddress,
        userAgent
          );

          TempData["StatusMessage"] = "? Registration successful! Your email has been verified.";
            return RedirectToPage("/Index");
    }

 // Registration failed
   foreach (var error in result.Errors)
    {
      ModelState.AddModelError(string.Empty, error.Description);
    }

   // Audit failed registration
      await _auditService.LogAsync(
 Email,
           Email,
         "Registration Failed",
     AuditStatus.Failed,
   $"Failed to create account: {string.Join(", ", result.Errors.Select(e => e.Description))}",
      ipAddress,
      userAgent
    );

  return Page();
   }

     public async Task<IActionResult> OnPostResendAsync()
   {
      var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

   _logger.LogInformation($"Resend OTP requested for {Email}");

    // Get the existing OTP token to retrieve registration data
            var existingToken = await _emailOtpService.ValidateOtpAsync(Email, "000000");
   // We pass dummy OTP to check if token exists
          
     // For resend, we'll need to retrieve the registration data differently
   // This is a simplified version - you may want to store it differently
        
   // Generate new OTP (this will invalidate old ones)
  var otpCode = await _emailOtpService.GenerateOtpAsync(Email, "{}", ipAddress ?? "Unknown");

    // Send OTP email
     var emailSent = await _emailService.SendOtpEmailAsync(Email, otpCode, "User");

    if (emailSent)
 {
         _logger.LogInformation($"OTP resent successfully to {Email}");
   
   // Audit OTP resend
     await _auditService.LogAsync(
                 Email,
     Email,
"OTP Resent",
   AuditStatus.Success,
      "New OTP code generated and sent",
   ipAddress,
   userAgent
 );

    TempData["StatusMessage"] = "? A new verification code has been sent to your email.";
          }
            else
  {
  _logger.LogError($"Failed to resend OTP to {Email}");
   ErrorMessage = "Failed to send verification code. Please try again.";
   }

       return Page();
   }
    }
}
