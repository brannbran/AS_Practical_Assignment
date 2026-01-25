using AS_Practical_Assignment.Models;
using AS_Practical_Assignment.Services;
using AS_Practical_Assignment.Validators;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AS_Practical_Assignment.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<Member> _userManager;
        private readonly SignInManager<Member> _signInManager;
        private readonly IEncryptionService _encryptionService;
        private readonly ISessionService _sessionService;
        private readonly IAuditService _auditService;
        private readonly IReCaptchaService _reCaptchaService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<Member> userManager,
            SignInManager<Member> signInManager,
            IEncryptionService encryptionService,
            ISessionService sessionService,
            IAuditService auditService,
            IReCaptchaService reCaptchaService,
            IWebHostEnvironment environment,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _encryptionService = encryptionService;
            _sessionService = sessionService;
            _auditService = auditService;
            _reCaptchaService = reCaptchaService;
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

            // Handle resume upload
            string? resumePath = null;
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

                // Create uploads directory if it doesn't exist
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "resumes");
                Directory.CreateDirectory(uploadsFolder);

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}_{Input.Resume.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.Resume.CopyToAsync(fileStream);
                }

                resumePath = Path.Combine("uploads", "resumes", uniqueFileName);
            }

            // Create member with ENCRYPTED customer data
            var member = new Member
            {
                UserName = Input.Email,
                Email = Input.Email,
                EncryptedFirstName = _encryptionService.Encrypt(Input.FirstName),
                EncryptedLastName = _encryptionService.Encrypt(Input.LastName),
                Gender = Input.Gender,
                EncryptedNRIC = _encryptionService.Encrypt(Input.NRIC),
                EncryptedDateOfBirth = _encryptionService.Encrypt(Input.DateOfBirth.ToString("yyyy-MM-dd")),
                ResumePath = resumePath,
                EncryptedWhoAmI = string.IsNullOrEmpty(Input.WhoAmI) ? null : _encryptionService.Encrypt(Input.WhoAmI),
                CreatedDate = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(member, Input.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");

                // Audit: User registration
                await _auditService.LogAsync(
                    member.Id,
                    member.Email ?? "",
                    AuditActions.Register,
                    AuditStatus.Success,
                    "New user registered successfully",
                    ipAddress,
                    userAgent
                );

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

                return RedirectToPage("/Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }
}
