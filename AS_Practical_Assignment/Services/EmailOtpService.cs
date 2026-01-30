using AS_Practical_Assignment.Data;
using AS_Practical_Assignment.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace AS_Practical_Assignment.Services
{
    public interface IEmailOtpService
    {
        Task<bool> CanGenerateOtpAsync(string email, string ipAddress);
     Task<string> GenerateOtpAsync(string email, string registrationDataJson, string ipAddress);
    Task<(bool isValid, EmailOtpToken? token)> ValidateOtpAsync(string email, string otpCode);
   Task MarkOtpAsUsedAsync(int tokenId);
        Task CleanupExpiredOtpsAsync();
}

    public class EmailOtpService : IEmailOtpService
    {
     private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailOtpService> _logger;

 private const int OTP_EXPIRY_MINUTES = 10; // OTP valid for 10 minutes
    private const int OTP_LENGTH = 6;
   private const int MAX_OTP_PER_EMAIL_PER_HOUR = 3; // Rate limit: 3 OTPs per email per hour
        private const int MAX_OTP_PER_IP_PER_HOUR = 5; // Rate limit: 5 OTPs per IP per hour

        public EmailOtpService(
    ApplicationDbContext context,
   ILogger<EmailOtpService> logger)
     {
   _context = context;
   _logger = logger;
      }

        /// <summary>
        /// Check if OTP can be generated (rate limiting)
     /// </summary>
        public async Task<bool> CanGenerateOtpAsync(string email, string ipAddress)
        {
      var oneHourAgo = DateTime.UtcNow.AddHours(-1);

  // Check email rate limit
    var otpsForEmail = await _context.EmailOtpTokens
                .Where(t => t.Email == email && t.CreatedDate > oneHourAgo)
                .CountAsync();

         if (otpsForEmail >= MAX_OTP_PER_EMAIL_PER_HOUR)
       {
                _logger.LogWarning("Rate limit exceeded for an email address. OTP limit per email reached within the last hour.");
                return false;
   }

            // Check IP rate limit
     var otpsForIp = await _context.EmailOtpTokens
      .Where(t => t.IpAddress == ipAddress && t.CreatedDate > oneHourAgo)
        .CountAsync();

            if (otpsForIp >= MAX_OTP_PER_IP_PER_HOUR)
     {
                _logger.LogWarning($"IP rate limit exceeded. {otpsForIp} OTPs in last hour.");
                return false;
       }

   return true;
   }

      /// <summary>
   /// Generate a 6-digit OTP code
   /// </summary>
  public async Task<string> GenerateOtpAsync(string email, string registrationDataJson, string ipAddress)
 {
          // Rate limiting check
if (!await CanGenerateOtpAsync(email, ipAddress))
     {
      throw new InvalidOperationException("Rate limit exceeded. Please try again later.");
   }

   // Generate cryptographically secure 6-digit code
    var otpCode = GenerateSecureOtp();

 // Invalidate any existing OTPs for this email
   var existingOtps = await _context.EmailOtpTokens
 .Where(o => o.Email == email && !o.IsUsed)
  .ToListAsync();

   if (existingOtps.Any())
  {
   _context.EmailOtpTokens.RemoveRange(existingOtps);
    _logger.LogInformation($"Invalidated {existingOtps.Count} existing OTP(s) for {email}");
    }

     // Create new OTP token
var otpToken = new EmailOtpToken
   {
    Email = email,
      OtpCode = otpCode,
    CreatedDate = DateTime.UtcNow,
   ExpiryDate = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES),
 IsUsed = false,
IpAddress = ipAddress,
      RegistrationDataJson = registrationDataJson
   };

  _context.EmailOtpTokens.Add(otpToken);
       await _context.SaveChangesAsync();

            _logger.LogInformation($"Generated OTP, expires at {otpToken.ExpiryDate}");

            return otpCode;
   }

      /// <summary>
 /// Validate OTP code
 /// </summary>
   public async Task<(bool isValid, EmailOtpToken? token)> ValidateOtpAsync(string email, string otpCode)
  {
    var otpToken = await _context.EmailOtpTokens
    .Where(o => o.Email == email && o.OtpCode == otpCode && !o.IsUsed)
      .OrderByDescending(o => o.CreatedDate)
      .FirstOrDefaultAsync();

  if (otpToken == null)
    {
                _logger.LogWarning("Invalid OTP code");
                return (false, null);
  }

     if (otpToken.IsUsed)
  {
                _logger.LogWarning("OTP already used");
                return (false, null);
  }

            if (DateTime.UtcNow > otpToken.ExpiryDate)
   {
                _logger.LogWarning("OTP expired");
                return (false, null);
 }

            _logger.LogInformation("Valid OTP provided");
            return (true, otpToken);
   }

   /// <summary>
     /// Mark OTP as used
  /// </summary>
   public async Task MarkOtpAsUsedAsync(int tokenId)
     {
     var otpToken = await _context.EmailOtpTokens.FindAsync(tokenId);
       if (otpToken != null)
   {
otpToken.IsUsed = true;
   await _context.SaveChangesAsync();
                _logger.LogInformation("Marked OTP as used");
            }
        }

        /// <summary>
    /// Cleanup expired OTPs (run periodically)
   /// </summary>
   public async Task CleanupExpiredOtpsAsync()
   {
      var expiredOtps = await _context.EmailOtpTokens
  .Where(o => o.ExpiryDate < DateTime.UtcNow || o.IsUsed)
     .ToListAsync();

   if (expiredOtps.Any())
{
 _context.EmailOtpTokens.RemoveRange(expiredOtps);
 await _context.SaveChangesAsync();
   _logger.LogInformation($"Cleaned up {expiredOtps.Count} expired/used OTP(s)");
       }
  }

   /// <summary>
   /// Generate cryptographically secure 6-digit OTP
        /// </summary>
private string GenerateSecureOtp()
 {
 using (var rng = RandomNumberGenerator.Create())
     {
       var bytes = new byte[4];
   rng.GetBytes(bytes);

  // Convert to integer and get 6 digits
   var number = BitConverter.ToUInt32(bytes, 0);
     var otp = (number % 1000000).ToString("D6");

  return otp;
   }
}
    }
}
