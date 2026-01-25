using AS_Practical_Assignment.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;

namespace AS_Practical_Assignment.Services
{
    public interface ITwoFactorService
    {
 string GenerateSecretKey();
     string GenerateQRCodeUrl(Member member, string secretKey);
  string GenerateTOTPCode(string secretKey);
    bool ValidateTOTPCode(string secretKey, string userCode);
     Task<bool> EnableTwoFactorAsync(Member member, string secretKey);
        Task<bool> DisableTwoFactorAsync(Member member);
   }

    public class TwoFactorService : ITwoFactorService
    {
        private readonly UserManager<Member> _userManager;
   private readonly ILogger<TwoFactorService> _logger;

private const string ISSUER = "AceJobAgency";
     private const int CODE_VALIDITY_WINDOW = 1; // Accept codes from 1 step before/after

   public TwoFactorService(
     UserManager<Member> userManager,
  ILogger<TwoFactorService> logger)
  {
   _userManager = userManager;
        _logger = logger;
  }

  /// <summary>
   /// Generate a new secret key for TOTP
   /// </summary>
   public string GenerateSecretKey()
   {
      var key = new byte[20];
 using (var rng = RandomNumberGenerator.Create())
      {
     rng.GetBytes(key);
      }
       return Base32Encode(key);
      }

/// <summary>
/// Generate QR code URL for authenticator apps
  /// </summary>
  public string GenerateQRCodeUrl(Member member, string secretKey)
        {
       var email = Uri.EscapeDataString(member.Email ?? "");
    var issuer = Uri.EscapeDataString(ISSUER);
   var otpauthUrl = $"otpauth://totp/{issuer}:{email}?secret={secretKey}&issuer={issuer}";
          
    // Return URL for QR code generation
            return $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString(otpauthUrl)}";
 }

  /// <summary>
        /// Generate TOTP code (for testing/validation)
        /// </summary>
 public string GenerateTOTPCode(string secretKey)
   {
       var counter = GetCurrentCounter();
return GenerateCode(secretKey, counter);
 }

  /// <summary>
 /// Validate user-provided TOTP code
 /// </summary>
 public bool ValidateTOTPCode(string secretKey, string userCode)
 {
    if (string.IsNullOrWhiteSpace(userCode) || userCode.Length != 6)
 {
     return false;
   }

   var currentCounter = GetCurrentCounter();

   // Check current code and codes within validity window
   for (int i = -CODE_VALIDITY_WINDOW; i <= CODE_VALIDITY_WINDOW; i++)
    {
     var code = GenerateCode(secretKey, currentCounter + i);
      if (code == userCode)
     {
 return true;
    }
       }

      return false;
 }

  /// <summary>
     /// Enable 2FA for a member
   /// </summary>
        public async Task<bool> EnableTwoFactorAsync(Member member, string secretKey)
 {
    try
     {
   member.TwoFactorSecretKey = secretKey;
       member.TwoFactorEnabled = true;

      var result = await _userManager.UpdateAsync(member);
     
       if (result.Succeeded)
      {
       _logger.LogInformation($"2FA enabled for user {member.Email}");
         return true;
  }
       else
       {
         _logger.LogError($"Failed to enable 2FA for user {member.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    return false;
 }
   }
   catch (Exception ex)
        {
_logger.LogError(ex, $"Error enabling 2FA for user {member.Email}");
      return false;
   }
      }

 /// <summary>
 /// Disable 2FA for a member
        /// </summary>
  public async Task<bool> DisableTwoFactorAsync(Member member)
 {
    try
   {
     member.TwoFactorSecretKey = null;
   member.TwoFactorEnabled = false;

var result = await _userManager.UpdateAsync(member);
            
         if (result.Succeeded)
    {
         _logger.LogInformation($"2FA disabled for user {member.Email}");
      return true;
   }
    else
 {
  _logger.LogError($"Failed to disable 2FA for user {member.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    return false;
      }
  }
     catch (Exception ex)
        {
       _logger.LogError(ex, $"Error disabling 2FA for user {member.Email}");
    return false;
 }
   }

  #region Private Helper Methods

   private long GetCurrentCounter()
    {
       var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
       return unixTimestamp / 30; // 30-second time step
 }

        private string GenerateCode(string secretKey, long counter)
 {
    var key = Base32Decode(secretKey);
      var counterBytes = BitConverter.GetBytes(counter);
          
            if (BitConverter.IsLittleEndian)
      {
    Array.Reverse(counterBytes);
 }

      using (var hmac = new HMACSHA1(key))
      {
       var hash = hmac.ComputeHash(counterBytes);
    var offset = hash[hash.Length - 1] & 0x0F;
 
      var binary = ((hash[offset] & 0x7F) << 24)
        | ((hash[offset + 1] & 0xFF) << 16)
       | ((hash[offset + 2] & 0xFF) << 8)
      | (hash[offset + 3] & 0xFF);

       var otp = binary % 1000000;
return otp.ToString("D6");
     }
        }

   private byte[] Base32Decode(string base32)
  {
     base32 = base32.ToUpper().Replace(" ", "");
       var output = new List<byte>();
      var buffer = 0;
 var bitsLeft = 0;

  foreach (var c in base32)
       {
    var value = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".IndexOf(c);
      if (value < 0) continue;

    buffer = (buffer << 5) | value;
      bitsLeft += 5;

      if (bitsLeft >= 8)
       {
         output.Add((byte)(buffer >> (bitsLeft - 8)));
         bitsLeft -= 8;
  }
       }

    return output.ToArray();
     }

   private string Base32Encode(byte[] data)
 {
 const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
   var result = new StringBuilder();
      var buffer = 0;
            var bitsLeft = 0;

            foreach (var b in data)
 {
  buffer = (buffer << 8) | b;
       bitsLeft += 8;

      while (bitsLeft >= 5)
    {
        result.Append(alphabet[(buffer >> (bitsLeft - 5)) & 31]);
       bitsLeft -= 5;
        }
   }

     if (bitsLeft > 0)
   {
       result.Append(alphabet[(buffer << (5 - bitsLeft)) & 31]);
  }

            return result.ToString();
  }

 #endregion
 }
}
