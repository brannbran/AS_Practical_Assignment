using AS_Practical_Assignment.Data;
using AS_Practical_Assignment.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AS_Practical_Assignment.Services
{
    public interface IPasswordPolicyService
    {
 Task<bool> CanChangePasswordAsync(Member member);
   Task<bool> IsPasswordExpiredAsync(Member member);
        Task<bool> IsPasswordReusedAsync(Member member, string newPassword);
        Task AddPasswordToHistoryAsync(Member member, string passwordHash);
  Task<(bool success, string? error)> ValidatePasswordPolicyAsync(Member member, string newPassword);
  Task SetPasswordExpiryAsync(Member member);
    }

    public class PasswordPolicyService : IPasswordPolicyService
    {
   private readonly ApplicationDbContext _context;
 private readonly IPasswordHasher<Member> _passwordHasher;
   private readonly ILogger<PasswordPolicyService> _logger;

    // Password policy configuration
        private const int PASSWORD_HISTORY_COUNT = 2; // Remember last 2 passwords
   private const int MINIMUM_PASSWORD_AGE_MINUTES = 1; // Cannot change password within 1 minute
        private const int MAXIMUM_PASSWORD_AGE_DAYS = 90; // Must change password after 90 days

        public PasswordPolicyService(
     ApplicationDbContext context,
IPasswordHasher<Member> passwordHasher,
   ILogger<PasswordPolicyService> logger)
   {
 _context = context;
 _passwordHasher = passwordHasher;
     _logger = logger;
        }

        /// <summary>
/// Check if user can change password (minimum password age)
   /// </summary>
        public async Task<bool> CanChangePasswordAsync(Member member)
        {
if (member.LastPasswordChangedDate == null)
     {
        return true; // First time password change
   }

  var timeSinceLastChange = DateTime.UtcNow - member.LastPasswordChangedDate.Value;

  if (timeSinceLastChange.TotalMinutes < MINIMUM_PASSWORD_AGE_MINUTES)
     {
    _logger.LogWarning($"User {member.Email} attempted to change password too soon. Last changed: {member.LastPasswordChangedDate}");
     return false;
            }

   return true;
        }

  /// <summary>
   /// Check if password has expired (maximum password age)
      /// </summary>
 public async Task<bool> IsPasswordExpiredAsync(Member member)
      {
if (member.PasswordExpiryDate == null)
         {
     return false;
   }

     return DateTime.UtcNow > member.PasswordExpiryDate.Value;
        }

        /// <summary>
        /// Check if new password was used recently (password history)
  /// </summary>
   public async Task<bool> IsPasswordReusedAsync(Member member, string newPassword)
 {
   // Get last N password hashes
       var passwordHistories = await _context.PasswordHistories
    .Where(ph => ph.MemberId == member.Id)
         .OrderByDescending(ph => ph.CreatedDate)
    .Take(PASSWORD_HISTORY_COUNT)
.ToListAsync();

 // Check if new password matches any previous password
     foreach (var history in passwordHistories)
     {
          var result = _passwordHasher.VerifyHashedPassword(member, history.PasswordHash, newPassword);
    if (result == PasswordVerificationResult.Success)
  {
         _logger.LogWarning($"User {member.Email} attempted to reuse a recent password");
     return true;
      }
   }

  return false;
        }

        /// <summary>
 /// Add current password to history
        /// </summary>
 public async Task AddPasswordToHistoryAsync(Member member, string passwordHash)
     {
        var passwordHistory = new PasswordHistory
            {
        MemberId = member.Id,
    PasswordHash = passwordHash,
        CreatedDate = DateTime.UtcNow
   };

       _context.PasswordHistories.Add(passwordHistory);

   // Clean up old history (keep only last N passwords)
       var oldHistories = await _context.PasswordHistories
    .Where(ph => ph.MemberId == member.Id)
       .OrderByDescending(ph => ph.CreatedDate)
    .Skip(PASSWORD_HISTORY_COUNT)
  .ToListAsync();

  if (oldHistories.Any())
   {
      _context.PasswordHistories.RemoveRange(oldHistories);
            }

            await _context.SaveChangesAsync();
       _logger.LogInformation($"Added password to history for user {member.Email}");
 }

        /// <summary>
        /// Validate all password policy rules
        /// </summary>
   public async Task<(bool success, string? error)> ValidatePasswordPolicyAsync(Member member, string newPassword)
        {
       // Check minimum password age
        if (!await CanChangePasswordAsync(member))
  {
     var minutesRemaining = MINIMUM_PASSWORD_AGE_MINUTES - 
(DateTime.UtcNow - member.LastPasswordChangedDate!.Value).TotalMinutes;
        return (false, $"You cannot change your password yet. Please wait {Math.Ceiling(minutesRemaining)} more minute(s).");
            }

       // Check password reuse
   if (await IsPasswordReusedAsync(member, newPassword))
      {
       return (false, $"You cannot reuse your last {PASSWORD_HISTORY_COUNT} passwords. Please choose a different password.");
     }

     return (true, null);
   }

   /// <summary>
   /// Set password expiry date
      /// </summary>
        public async Task SetPasswordExpiryAsync(Member member)
        {
 member.LastPasswordChangedDate = DateTime.UtcNow;
member.PasswordExpiryDate = DateTime.UtcNow.AddDays(MAXIMUM_PASSWORD_AGE_DAYS);

   await _context.SaveChangesAsync();
   _logger.LogInformation($"Set password expiry for user {member.Email} to {member.PasswordExpiryDate}");
        }
 }
}
