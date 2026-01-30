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
        // Helper to avoid logging email addresses in logs
        private static string RedactEmail(string? email)
        {
            return "[redacted]";
        }
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
       _logger.LogWarning($"ModelState invalid for email: {Email}");
  return Page();
          }

     var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
         var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            _logger.LogInformation($"[STEP 1] Starting OTP validation for {Email}");

 // Validate OTP
       var (isValid, otpToken) = await _emailOtpService.ValidateOtpAsync(Email, Input.OtpCode);

    _logger.LogInformation($"[STEP 2] OTP validation result for {Email}: isValid={isValid}, token={otpToken != null}");

     if (!isValid || otpToken == null)
        {
      _logger.LogWarning($"[STEP 2-FAIL] Invalid OTP attempt for {Email}");

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

   _logger.LogInformation($"[STEP 3] Deserializing registration data for {Email}");

 // Deserialize registration data
  RegistrationTempData? regData;
   try
   {
      regData = JsonSerializer.Deserialize<RegistrationTempData>(otpToken.RegistrationDataJson!);
     if (regData == null)
    {
       throw new Exception("Failed to deserialize registration data");
             }
                _logger.LogInformation($"[STEP 3-SUCCESS] Deserialized data for {RedactEmail(Email)}: FirstName={regData.FirstName}");
            }
     catch (Exception ex)
      {
                _logger.LogError(ex, $"[STEP 3-FAIL] Failed to deserialize registration data for {RedactEmail(Email)}");
                return Page();
         }

            _logger.LogInformation($"[STEP 4] Checking if user already exists for {RedactEmail(Email)}");

            // Check if email already exists (with detailed logging)
            var existingUser = await _userManager.FindByEmailAsync(Email);
       
            if (existingUser != null)
     {
                _logger.LogWarning($"[STEP 4-DUPLICATE] Duplicate email detected during OTP verification: {RedactEmail(Email)}");
                _logger.LogWarning($"[STEP 4-DUPLICATE] Existing user ID: {existingUser.Id}, Created: {existingUser.CreatedDate}");
        
     // Mark OTP as used since it was valid (prevent reuse)
           await _emailOtpService.MarkOtpAsUsedAsync(otpToken.Id);
   
  // Audit the attempt
      await _auditService.LogAsync(
 existingUser.Id,
  Email,
 "Duplicate Registration Attempt",
      AuditStatus.Failed,
    "Valid OTP provided but email already registered",
    ipAddress,
    userAgent
        );
  
        // Clear and specific error message
       ModelState.AddModelError(string.Empty, 
        "This email address is already registered. If you already have an account, please login instead.");
      
        // Store message in TempData for visibility
     TempData["ErrorType"] = "DuplicateEmail";
      TempData["ErrorMessage"] = $"The email '{Email}' is already registered. If this is your account, please use the Login page instead.";
  
           return Page();
   }

            _logger.LogInformation($"[STEP 4-CLEAR] No existing user found for {RedactEmail(Email)}");

            // Also check by normalized email (case-insensitive)
            var normalizedEmail = _userManager.NormalizeEmail(Email);
            _logger.LogInformation("[STEP 5] Checking for existing user using normalized email.");

            if (!string.IsNullOrEmpty(normalizedEmail))
        {
    var existingUserByNormalized = await _userManager.FindByEmailAsync(normalizedEmail);
     if (existingUserByNormalized != null && existingUserByNormalized.Id != existingUser?.Id)
  {
                    _logger.LogWarning("[STEP 5-DUPLICATE] Duplicate normalized email detected for attempted registration.");
                    await _emailOtpService.MarkOtpAsUsedAsync(otpToken.Id);
      
 ModelState.AddModelError(string.Empty, 
             "This email address is already registered. If you already have an account, please login instead.");
      
   TempData["ErrorType"] = "DuplicateEmail";
     TempData["ErrorMessage"] = $"The email '{Email}' is already registered. Please login instead.";
   
        return Page();
        }
        }

            _logger.LogInformation($"[STEP 6] Proceeding with account creation for {RedactEmail(Email)}");

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
                    _logger.LogInformation($"[STEP 6-RESUME] Saved resume for {Email}: {uniqueFileName}");
 }
      catch (Exception ex)
        {
     _logger.LogError(ex, $"[STEP 6-RESUME-FAIL] Failed to save resume for {Email}");
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

            _logger.LogInformation($"[STEP 7] Attempting to create user account for {Email}");

        var result = await _userManager.CreateAsync(member, regData.Password);

            _logger.LogInformation($"[STEP 8] CreateAsync result for {Email}: Succeeded={result.Succeeded}");

   if (result.Succeeded)
          {
  _logger.LogInformation($"[STEP 9-SUCCESS] User {Email} created account successfully after OTP verification");

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

            // Registration failed - log detailed error information
      _logger.LogError($"[STEP 9-FAIL] User creation failed for {Email}");
      foreach (var error in result.Errors)
  {
        _logger.LogError($"[STEP 9-FAIL] Identity Error: {error.Code} - {error.Description}");
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
