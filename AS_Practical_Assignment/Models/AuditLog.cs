using System.ComponentModel.DataAnnotations;

namespace AS_Practical_Assignment.Models
{
    public class AuditLog
    {
    [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

        [StringLength(256)]
        public string? UserEmail { get; set; }

        [Required]
  [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

 [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = string.Empty; // Success, Failed, Warning

  [StringLength(1000)]
        public string? AdditionalInfo { get; set; }
    }

    // Audit Action Constants
  public static class AuditActions
    {
        public const string Login = "Login";
        public const string LoginFailed = "Login Failed";
   public const string Logout = "Logout";
        public const string Register = "Register";
        public const string PasswordChange = "Password Change";
        public const string ProfileUpdate = "Profile Update";
        public const string SessionExpired = "Session Expired";
        public const string MultipleLogin = "Multiple Login Detected";
        public const string AccountLocked = "Account Locked";
        public const string DataAccess = "Data Access";
        public const string DataModification = "Data Modification";
    }

    // Audit Status Constants
    public static class AuditStatus
    {
        public const string Success = "Success";
        public const string Failed = "Failed";
        public const string Warning = "Warning";
        public const string Info = "Info";
    }
}
