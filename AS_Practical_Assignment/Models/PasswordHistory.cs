using System.ComponentModel.DataAnnotations;

namespace AS_Practical_Assignment.Models
{
    /// <summary>
    /// Stores password history for a member to prevent password reuse
    /// </summary>
    public class PasswordHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
 public string MemberId { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

     public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

   // Navigation property
        public virtual Member? Member { get; set; }
    }
}
