using AS_Practical_Assignment.Models;
using AS_Practical_Assignment.Services;
using AS_Practical_Assignment.Validators;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace AS_Practical_Assignment.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<Member> _userManager;
        private readonly IEncryptionService _encryptionService;
        private readonly IAuditService _auditService;
        private readonly IReCaptchaService _reCaptchaService;
        private readonly IEmailService _emailService;
        private readonly IEmailOtpService _emailOtpService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<Member> userManager,
            IEncryptionService encryptionService,
            IAuditService auditService,
            IReCaptchaService reCaptchaService,
            IEmailService emailService,
            IEmailOtpService emailOtpService,
            IWebHostEnvironment environment,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _encryptionService = encryptionService;
            _auditService = auditService;
            _reCaptchaService = reCaptchaService;
            _emailService = emailService;
            _emailOtpService = emailOtpService;
            _environment = environment;
            _logger = logger;
        }

        [BindProperty]
        public RegisterViewModel Input { get; set; } = new RegisterViewModel();

        public string? ErrorMessage { get; set; }
        public string PasswordStrength { get; set; } = string.Empty;
        public string ReCaptchaSiteKey { get; set; } = string.Empty;

        public void OnGet()
        {
            // Get reCAPTCHA site key
            ReCaptchaSiteKey = _reCaptchaService.GetSiteKey();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Get reCAPTCHA site key for re-rendering if validation fails
            ReCaptchaSiteKey = _reCaptchaService.GetSiteKey();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            // Validate reCAPTCHA
            var reCaptchaToken = Request.Form["g-recaptcha-response"].ToString();
            var reCaptchaResult = await _reCaptchaService.ValidateAsync(reCaptchaToken, "register", ipAddress);

            if (!reCaptchaResult.IsValid)
            {
                _logger.LogWarning($"reCAPTCHA validation failed for registration: {Input.Email}. Score: {reCaptchaResult.Score}");

                // Audit: reCAPTCHA failed
                await _auditService.LogAsync(
                    Input.Email,
                    Input.Email,
                    "reCAPTCHA Failed",
                    AuditStatus.Failed,
                    $"Bot detection failed during registration. {reCaptchaResult.Message}",
                    ipAddress,
                    userAgent,
                    $"Score: {reCaptchaResult.Score}"
                );

                ModelState.AddModelError(string.Empty, "Bot detection failed. Please try again. If you believe this is an error, contact support.");
                return Page();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Server-side password validation
            var (isValid, errors) = StrongPasswordAttribute.ValidatePassword(Input.Password);
            if (!isValid)
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError("Input.Password", error);
                }
                return Page();
            }

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "This email address is already registered. Please use a different email.");
                return Page();
            }

            // Handle resume upload and convert to byte array for temporary storage
            byte[]? resumeData = null;
            string? resumeFileName = null;
            if (Input.Resume != null)
            {
                var allowedExtensions = new[] { ".pdf", ".docx" };
                var extension = Path.GetExtension(Input.Resume.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("Input.Resume", "Only .pdf or .docx files are allowed.");
                    return Page();
                }

                // File size validation - max 5 MB
                const long maxFileSize = 5 * 1024 * 1024; // 5 MB
                if (Input.Resume.Length > maxFileSize)
                {
                    ModelState.AddModelError("Input.Resume", "File size cannot exceed 5 MB.");
                    return Page();
                }

                // Convert resume to byte array
                using (var memoryStream = new MemoryStream())
                {
                    await Input.Resume.CopyToAsync(memoryStream);
                    resumeData = memoryStream.ToArray();
                    resumeFileName = Input.Resume.FileName;
                }
            }

            // Create temporary registration data
            var tempData = new RegistrationTempData
            {
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                Gender = Input.Gender,
                NRIC = Input.NRIC,
                DateOfBirth = Input.DateOfBirth,
                Email = Input.Email,
                Password = Input.Password,
                WhoAmI = Input.WhoAmI,
                ResumeFileName = resumeFileName,
                ResumeData = resumeData
            };

            // Serialize registration data
            var tempDataJson = JsonSerializer.Serialize(tempData);

            // Generate OTP
            var otpCode = await _emailOtpService.GenerateOtpAsync(Input.Email, tempDataJson, ipAddress ?? "Unknown");

            // Send OTP email
            var emailSent = await _emailService.SendOtpEmailAsync(Input.Email, otpCode, Input.FirstName);

            if (!emailSent)
            {
                _logger.LogError($"Failed to send OTP email to {Input.Email}");
                ModelState.AddModelError(string.Empty, "Failed to send verification email. Please try again.");

                // Audit failed email send
                await _auditService.LogAsync(
                    Input.Email,
                    Input.Email,
                    "OTP Email Failed",
                    AuditStatus.Failed,
                    "Failed to send OTP verification email",
                    ipAddress,
                    userAgent
                );

                return Page();
            }

            _logger.LogInformation($"OTP sent successfully to {Input.Email}");

            // Audit: OTP sent
            await _auditService.LogAsync(
                Input.Email,
                Input.Email,
                "OTP Sent",
                AuditStatus.Success,
                "Email verification code sent",
                ipAddress,
                userAgent
            );

            // Redirect to verification page
            return RedirectToPage("/VerifyEmail", new { email = Input.Email });
        }
    }
}
