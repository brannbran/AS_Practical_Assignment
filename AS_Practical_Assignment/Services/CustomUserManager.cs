using AS_Practical_Assignment.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AS_Practical_Assignment.Services
{
    /// <summary>
  /// Custom UserManager that uses our enhanced password hasher with custom salting
  /// </summary>
 public class CustomUserManager : UserManager<Member>
    {
  private readonly ICustomPasswordHasher _customPasswordHasher;

        public CustomUserManager(
   IUserStore<Member> store,
   IOptions<IdentityOptions> optionsAccessor,
   IPasswordHasher<Member> passwordHasher,
 IEnumerable<IUserValidator<Member>> userValidators,
   IEnumerable<IPasswordValidator<Member>> passwordValidators,
     ILookupNormalizer keyNormalizer,
 IdentityErrorDescriber errors,
    IServiceProvider services,
     ILogger<UserManager<Member>> logger,
  ICustomPasswordHasher customPasswordHasher)
: base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
   {
     _customPasswordHasher = customPasswordHasher;
        }

  /// <summary>
     /// Override CreateAsync to use custom password hasher
        /// </summary>
        public override async Task<IdentityResult> CreateAsync(Member user, string password)
 {
       ThrowIfDisposed();
      
     if (user == null)
       {
      throw new ArgumentNullException(nameof(user));
   }

     if (string.IsNullOrWhiteSpace(password))
   {
    throw new ArgumentNullException(nameof(password));
  }

       // Validate password
      var validationResult = await ValidatePasswordAsync(user, password);
    if (!validationResult.Succeeded)
   {
   return validationResult;
  }

     // Hash password with custom salting
    user.PasswordHash = _customPasswordHasher.HashPassword(user, password);

   // Create user
    var result = await base.CreateAsync(user);

      return result;
  }

    /// <summary>
   /// Override ChangePasswordAsync to use custom password hasher
     /// </summary>
 public override async Task<IdentityResult> ChangePasswordAsync(Member user, string currentPassword, string newPassword)
        {
     ThrowIfDisposed();

      if (user == null)
   {
     throw new ArgumentNullException(nameof(user));
  }

  // Verify current password
   var verificationResult = _customPasswordHasher.VerifyHashedPassword(user, user.PasswordHash ?? "", currentPassword);
            
    if (verificationResult == PasswordVerificationResult.Failed)
   {
      return IdentityResult.Failed(new IdentityError
     {
      Code = "PasswordMismatch",
      Description = "Incorrect password."
   });
 }

       // Validate new password
  var validationResult = await ValidatePasswordAsync(user, newPassword);
if (!validationResult.Succeeded)
   {
  return validationResult;
  }

     // Hash new password
       user.PasswordHash = _customPasswordHasher.HashPassword(user, newPassword);

    // Update security stamp
       await UpdateSecurityStampAsync(user);

      // Save changes
     var result = await UpdateAsync(user);

     return result;
 }

        /// <summary>
   /// Override ResetPasswordAsync to use custom password hasher
 /// </summary>
        public override async Task<IdentityResult> ResetPasswordAsync(Member user, string token, string newPassword)
        {
    ThrowIfDisposed();

      if (user == null)
  {
  throw new ArgumentNullException(nameof(user));
  }

            // Verify token
       var verifyResult = await base.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, "ResetPassword", token);
    if (!verifyResult)
  {
  return IdentityResult.Failed(new IdentityError
    {
    Code = "InvalidToken",
 Description = "Invalid token."
   });
 }

   // Validate password
       var validationResult = await ValidatePasswordAsync(user, newPassword);
if (!validationResult.Succeeded)
  {
    return validationResult;
   }

   // Hash new password with custom salting
  user.PasswordHash = _customPasswordHasher.HashPassword(user, newPassword);

     // Update security stamp
  await UpdateSecurityStampAsync(user);

   // Save changes
       var result = await UpdateAsync(user);

   return result;
 }

        /// <summary>
        /// Override CheckPasswordAsync to use custom password hasher
 /// </summary>
        public override async Task<bool> CheckPasswordAsync(Member user, string password)
 {
 ThrowIfDisposed();

   if (user == null)
            {
       return false;
      }

       if (string.IsNullOrWhiteSpace(user.PasswordHash))
 {
       return false;
            }

            var result = _customPasswordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
   
          return result == PasswordVerificationResult.Success || 
   result == PasswordVerificationResult.SuccessRehashNeeded;
  }
    }
}
