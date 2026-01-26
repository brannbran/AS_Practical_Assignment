using System.ComponentModel.DataAnnotations;

namespace AS_Practical_Assignment.Models
{
    /// <summary>
    /// Stores email OTP codes for registration verification
    /// </summary>
    public class EmailOtpToken
  {
        [Key]
        public int Id { get; set; }

     [Required]
[EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
[StringLength(6, MinimumLength = 6)]
  public string OtpCode { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime ExpiryDate { get; set; }

    public bool IsUsed { get; set; } = false;

        public string? IpAddress { get; set; }

        /// <summary>
        /// Temporary storage for registration data (encrypted)
      /// </summary>
        public string? RegistrationDataJson { get; set; }
    }
}
