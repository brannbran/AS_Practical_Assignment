using System.ComponentModel.DataAnnotations;

namespace AS_Practical_Assignment.Models
{
    /// <summary>
    /// Temporary model to store registration data during OTP verification
    /// </summary>
    public class RegistrationTempData
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
  public string Gender { get; set; } = string.Empty;

        [Required]
  public string NRIC { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

   [Required]
   public string Password { get; set; } = string.Empty;

        public string? WhoAmI { get; set; }

   public string? ResumeFileName { get; set; }
public byte[]? ResumeData { get; set; }
    }
}
