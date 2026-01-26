namespace AS_Practical_Assignment.Models
{
  /// <summary>
    /// Email configuration settings from appsettings.json
 /// </summary>
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
 public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
      public string Password { get; set; } = string.Empty;
      public bool EnableSsl { get; set; } = true;
    }
}
