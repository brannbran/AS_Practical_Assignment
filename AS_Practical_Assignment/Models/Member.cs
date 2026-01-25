using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AS_Practical_Assignment.Models
{
    public class Member : IdentityUser
    {
        [Required]
        [StringLength(500)] // Encrypted data will be longer
        public string EncryptedFirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)] // Encrypted data will be longer
        public string EncryptedLastName { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Gender { get; set; } = string.Empty;

        [Required]
        [StringLength(500)] // Encrypted NRIC will be longer than plain text
        public string EncryptedNRIC { get; set; } = string.Empty;

        [Required]
        [StringLength(500)] // Encrypted date will be longer
        public string EncryptedDateOfBirth { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ResumePath { get; set; }

        [StringLength(5000)] // Encrypted data will be longer
        public string? EncryptedWhoAmI { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Session management properties
        [StringLength(500)]
        public string? CurrentSessionId { get; set; }

        public DateTime? LastLoginDate { get; set; }

        public DateTime? LastActivityDate { get; set; }

        [StringLength(100)]
        public string? LastLoginIP { get; set; }

        [StringLength(500)]
        public string? LastUserAgent { get; set; }

        // Password policy properties
        public DateTime? LastPasswordChangedDate { get; set; }

        public DateTime? PasswordExpiryDate { get; set; }

        // 2FA properties
        public bool TwoFactorEnabled { get; set; } = false;

        [StringLength(500)]
        public string? TwoFactorSecretKey { get; set; }

        // Navigation properties
        public virtual ICollection<PasswordHistory> PasswordHistories { get; set; } = new List<PasswordHistory>();

        public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    }
}
