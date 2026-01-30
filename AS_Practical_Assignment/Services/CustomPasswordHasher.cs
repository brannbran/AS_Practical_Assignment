using AS_Practical_Assignment.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;

namespace AS_Practical_Assignment.Services
{
 /// <summary>
    /// Custom password hasher with additional salting layer for enhanced security
    /// </summary>
  public interface ICustomPasswordHasher
    {
     string HashPassword(Member user, string password);
        PasswordVerificationResult VerifyHashedPassword(Member user, string hashedPassword, string providedPassword);
string GenerateSalt();
    }

    public class CustomPasswordHasher : ICustomPasswordHasher
    {
        private readonly IPasswordHasher<Member> _defaultHasher;
   private readonly ILogger<CustomPasswordHasher> _logger;
        private const int SALT_SIZE = 32; // 32 bytes = 256 bits

        public CustomPasswordHasher(
     IPasswordHasher<Member> defaultHasher,
      ILogger<CustomPasswordHasher> logger)
  {
       _defaultHasher = defaultHasher;
       _logger = logger;
  }

      /// <summary>
        /// Generate a cryptographically secure random salt
        /// </summary>
        public string GenerateSalt()
     {
            var saltBytes = new byte[SALT_SIZE];
            using (var rng = RandomNumberGenerator.Create())
            {
         rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

   /// <summary>
        /// Hash password with custom salt + Identity's built-in hashing
        /// </summary>
        public string HashPassword(Member user, string password)
        {
            // Step 1: Generate a unique salt for this user
          var salt = GenerateSalt();

       // Step 2: Combine password with salt
var saltedPassword = CombinePasswordAndSalt(password, salt);

 // Step 3: Use Identity's default hasher (which adds its own salt)
    var hashedPassword = _defaultHasher.HashPassword(user, saltedPassword);

 // Step 4: Combine our custom salt with the hashed password
// Format: {CustomSalt}:{IdentityHash}
    var finalHash = $"{salt}:{hashedPassword}";

      _logger.LogInformation($"Password hashed with custom salt for user {user.Email}");

   return finalHash;
        }

        /// <summary>
     /// Verify password against stored hash
        /// </summary>
   public PasswordVerificationResult VerifyHashedPassword(Member user, string hashedPassword, string providedPassword)
  {
            try
 {
        // Step 1: Extract custom salt and Identity hash
                var parts = hashedPassword.Split(':', 2);
           if (parts.Length != 2)
     {
  _logger.LogWarning($"Invalid password hash format for user {user.Email}");
         return PasswordVerificationResult.Failed;
              }

      var customSalt = parts[0];
       var identityHash = parts[1];

        // Step 2: Combine provided password with custom salt
                var saltedPassword = CombinePasswordAndSalt(providedPassword, customSalt);

    // Step 3: Verify using Identity's hasher
    var result = _defaultHasher.VerifyHashedPassword(user, identityHash, saltedPassword);

      if (result == PasswordVerificationResult.Success)
 {
        _logger.LogInformation($"Password verified successfully for user {user.Email}");
     }
             else
         {
               _logger.LogWarning($"Password verification failed for user {user.Email}");
    }

    return result;
       }
       catch (Exception ex)
     {
          _logger.LogError(ex, $"Error verifying password for user {user.Email}");
       return PasswordVerificationResult.Failed;
            }
        }

  /// <summary>
        /// Combine password and salt using PBKDF2
        /// </summary>
     private string CombinePasswordAndSalt(string password, string salt)
        {
  // Use PBKDF2 to combine password and salt
  var saltBytes = Convert.FromBase64String(salt);
    var passwordBytes = Encoding.UTF8.GetBytes(password);

    using (var pbkdf2 = new Rfc2898DeriveBytes(
 password: passwordBytes,
              salt: saltBytes,
                iterations: 10000, // 10,000 iterations
   hashAlgorithm: HashAlgorithmName.SHA256))
      {
         var derivedKey = pbkdf2.GetBytes(32); // 32 bytes = 256 bits
   return Convert.ToBase64String(derivedKey);
     }
        }
    }
}
