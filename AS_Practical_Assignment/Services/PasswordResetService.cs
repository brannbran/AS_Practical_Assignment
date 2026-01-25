using AS_Practical_Assignment.Data;
using AS_Practical_Assignment.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace AS_Practical_Assignment.Services
{
    public interface IPasswordResetService
 {
 Task<string> GeneratePasswordResetTokenAsync(Member member, string ipAddress);
  Task<(bool isValid, Member? member)> ValidateResetTokenAsync(string token);
   Task MarkTokenAsUsedAsync(string token);
        Task CleanupExpiredTokensAsync();
    }

    public class PasswordResetService : IPasswordResetService
    {
 private readonly ApplicationDbContext _context;
      private readonly ILogger<PasswordResetService> _logger;

     private const int TOKEN_EXPIRY_HOURS = 1; // Reset token valid for 1 hour

    public PasswordResetService(
  ApplicationDbContext context,
  ILogger<PasswordResetService> logger)
        {
 _context = context;
   _logger = logger;
 }

 /// <summary>
        /// Generate a secure password reset token
        /// </summary>
 public async Task<string> GeneratePasswordResetTokenAsync(Member member, string ipAddress)
        {
      // Generate cryptographically secure token
   var token = GenerateSecureToken();

     var resetToken = new PasswordResetToken
          {
     MemberId = member.Id,
 Token = token,
    CreatedDate = DateTime.UtcNow,
 ExpiryDate = DateTime.UtcNow.AddHours(TOKEN_EXPIRY_HOURS),
  IsUsed = false,
  IpAddress = ipAddress
      };

   _context.PasswordResetTokens.Add(resetToken);
    await _context.SaveChangesAsync();

   _logger.LogInformation($"Generated password reset token for user {member.Email}");

      return token;
 }

        /// <summary>
 /// Validate password reset token
        /// </summary>
  public async Task<(bool isValid, Member? member)> ValidateResetTokenAsync(string token)
 {
   var resetToken = await _context.PasswordResetTokens
.Include(rt => rt.Member)
      .FirstOrDefaultAsync(rt => rt.Token == token);

   if (resetToken == null)
      {
         _logger.LogWarning($"Invalid reset token: {token}");
    return (false, null);
      }

       if (resetToken.IsUsed)
  {
         _logger.LogWarning($"Reset token already used: {token}");
    return (false, null);
     }

     if (DateTime.UtcNow > resetToken.ExpiryDate)
   {
         _logger.LogWarning($"Reset token expired: {token}");
       return (false, null);
    }

  return (true, resetToken.Member);
        }

 /// <summary>
   /// Mark token as used
 /// </summary>
        public async Task MarkTokenAsUsedAsync(string token)
 {
       var resetToken = await _context.PasswordResetTokens
    .FirstOrDefaultAsync(rt => rt.Token == token);

    if (resetToken != null)
       {
     resetToken.IsUsed = true;
         await _context.SaveChangesAsync();
   _logger.LogInformation($"Marked reset token as used: {token}");
  }
 }

        /// <summary>
  /// Cleanup expired tokens (run periodically)
        /// </summary>
   public async Task CleanupExpiredTokensAsync()
    {
     var expiredTokens = await _context.PasswordResetTokens
    .Where(rt => rt.ExpiryDate < DateTime.UtcNow || rt.IsUsed)
      .ToListAsync();

      if (expiredTokens.Any())
       {
      _context.PasswordResetTokens.RemoveRange(expiredTokens);
       await _context.SaveChangesAsync();
    _logger.LogInformation($"Cleaned up {expiredTokens.Count} expired/used reset tokens");
  }
   }

   private string GenerateSecureToken()
 {
   // Generate 32-byte random token
            var randomBytes = new byte[32];
using (var rng = RandomNumberGenerator.Create())
       {
      rng.GetBytes(randomBytes);
       }

  // Convert to Base64 URL-safe string
     return Convert.ToBase64String(randomBytes)
    .Replace('+', '-')
    .Replace('/', '_')
    .TrimEnd('=');
        }
  }
}
