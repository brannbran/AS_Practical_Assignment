using System.ComponentModel.DataAnnotations;

namespace AS_Practical_Assignment.Models
{
    /// <summary>
    /// Stores password reset tokens for email/SMS-based password recovery
    /// </summary>
    public class PasswordResetToken
    {
    [Key]
        public int Id { get; set; }

      [Required]
        public string MemberId { get; set; } = string.Empty;

        [Required]
  public string Token { get; set; } = string.Empty;

   public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime ExpiryDate { get; set; }

 public bool IsUsed { get; set; } = false;

        public string? IpAddress { get; set; }

        // Navigation property
        public virtual Member? Member { get; set; }
    }
}
