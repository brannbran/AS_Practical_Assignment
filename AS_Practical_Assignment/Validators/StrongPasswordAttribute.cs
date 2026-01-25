using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace AS_Practical_Assignment.Validators
{
    public class StrongPasswordAttribute : ValidationAttribute
    {
  public int MinimumLength { get; set; } = 12;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
     {
       if (value == null || string.IsNullOrEmpty(value.ToString()))
       {
          return new ValidationResult("Password is required.");
  }

         string password = value.ToString()!;

       if (password.Length < MinimumLength)
{
                return new ValidationResult($"Password must be at least {MinimumLength} characters long.");
          }

            if (!Regex.IsMatch(password, @"[a-z]"))
            {
         return new ValidationResult("Password must contain at least one lowercase letter (a-z).");
            }

 if (!Regex.IsMatch(password, @"[A-Z]"))
            {
      return new ValidationResult("Password must contain at least one uppercase letter (A-Z).");
      }

    if (!Regex.IsMatch(password, @"\d"))
     {
    return new ValidationResult("Password must contain at least one digit (0-9).");
            }

if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?]"))
            {
     return new ValidationResult("Password must contain at least one special character (!@#$%^&* etc.).");
            }

            return ValidationResult.Success;
        }

        public static string GetPasswordStrength(string password)
        {
     if (string.IsNullOrEmpty(password))
                return "Empty";

            int score = 0;

// Length score
         if (password.Length >= 8) score++;
         if (password.Length >= 12) score++;
 if (password.Length >= 16) score++;

     // Character variety score
      if (Regex.IsMatch(password, @"[a-z]")) score++;
            if (Regex.IsMatch(password, @"[A-Z]")) score++;
    if (Regex.IsMatch(password, @"\d")) score++;
        if (Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?]")) score++;

       return score switch
            {
    <= 2 => "Weak",
          <= 4 => "Fair",
       <= 6 => "Good",
_ => "Strong"
    };
        }

        public static (bool isValid, List<string> errors) ValidatePassword(string password)
        {
            var errors = new List<string>();

       if (string.IsNullOrEmpty(password))
          {
       errors.Add("Password is required");
    return (false, errors);
        }

          if (password.Length < 12)
     errors.Add("Minimum 12 characters required");

            if (!Regex.IsMatch(password, @"[a-z]"))
      errors.Add("Must contain lowercase letter");

     if (!Regex.IsMatch(password, @"[A-Z]"))
      errors.Add("Must contain uppercase letter");

        if (!Regex.IsMatch(password, @"\d"))
        errors.Add("Must contain a digit");

     if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>/?]"))
                errors.Add("Must contain special character");

            return (errors.Count == 0, errors);
        }
    }
}
