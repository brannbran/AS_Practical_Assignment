using AS_Practical_Assignment.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace AS_Practical_Assignment.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
 Task<bool> SendOtpEmailAsync(string toEmail, string otpCode, string userName);
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetUrl);
        Task<bool> SendWelcomeEmailAsync(string toEmail, string userName);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;
        private readonly IWebHostEnvironment _environment;

   public EmailService(
          IOptions<EmailSettings> emailSettings,
            ILogger<EmailService> logger,
            IWebHostEnvironment environment)
        {
 _emailSettings = emailSettings.Value;
         _logger = logger;
_environment = environment;
        }

     /// <summary>
 /// Send email using SMTP (or console in development)
      /// </summary>
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
     // In development, log to console instead of sending email
            if (_environment.IsDevelopment())
            {
      _logger.LogInformation("==============================================");
                _logger.LogInformation("?? EMAIL (DEVELOPMENT MODE - NOT ACTUALLY SENT)");
        _logger.LogInformation("==============================================");
          _logger.LogInformation($"To: {toEmail}");
       _logger.LogInformation($"Subject: {subject}");
         _logger.LogInformation($"Body (HTML): {htmlBody}");
     _logger.LogInformation("==============================================");
   
          // Simulate async operation
       await Task.Delay(100);
  return true;
       }

            // Production: Actually send email via SMTP
      try
{
                using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort);
        client.EnableSsl = _emailSettings.EnableSsl;
     client.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);

        var mailMessage = new MailMessage
     {
          From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
    Subject = subject,
       Body = htmlBody,
    IsBodyHtml = true
     };

  mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);

  _logger.LogInformation($"Email sent successfully to {toEmail}");
return true;
     }
            catch (Exception ex)
            {
          _logger.LogError(ex, $"Failed to send email to {toEmail}");
    return false;
         }
     }

        /// <summary>
        /// Send OTP verification email (or log to console)
  /// </summary>
   public async Task<bool> SendOtpEmailAsync(string toEmail, string otpCode, string userName)
        {
            // In development, display OTP prominently in console
   if (_environment.IsDevelopment())
            {
 _logger.LogWarning("??????????????????????????????????????????????????");
          _logger.LogWarning("?   ?");
          _logger.LogWarning("?          ?? OTP VERIFICATION CODE       ?");
          _logger.LogWarning("?                    ?");
   _logger.LogWarning("??????????????????????????????????????????????????");
   _logger.LogWarning($"?  Email:  {toEmail,-37} ?");
      _logger.LogWarning($"?  Name:   {userName,-37} ?");
      _logger.LogWarning("?  ?");
                _logger.LogWarning($"?  CODE:   {otpCode,-37} ?");
 _logger.LogWarning("?         ?");
      _logger.LogWarning("?  Valid for: 10 minutes     ?");
      _logger.LogWarning("??????????????????????????????????????????????????");
                
                // Also log plain for easy copy-paste
_logger.LogWarning($"?? COPY THIS CODE: {otpCode}");
 
     await Task.Delay(100);
          return true;
 }

            // Production: Send actual email
 var subject = "Your Verification Code - Ace Job Agency";
 
       var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
   .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
 .header {{ background-color: #0d6efd; color: white; padding: 20px; text-align: center; }}
  .content {{ padding: 30px; background-color: #f8f9fa; }}
        .otp-box {{ background-color: white; border: 2px dashed #0d6efd; padding: 20px; text-align: center; margin: 20px 0; }}
   .otp-code {{ font-size: 32px; font-weight: bold; color: #0d6efd; letter-spacing: 5px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
      .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 10px, 15px, 10px, 0; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>?? Email Verification</h1>
   </div>
        <div class='content'>
         <p>Hello <strong>{userName}</strong>,</p>
            <p>Thank you for registering with Ace Job Agency! To complete your registration, please verify your email address.</p>
        
   <div class='otp-box'>
       <p style='margin: 0; font-size: 14px; color: #666;'>Your Verification Code:</p>
 <div class='otp-code'>{otpCode}</div>
                <p style='margin: 10px 0 0 0; font-size: 12px; color: #999;'>Valid for 10 minutes</p>
            </div>

            <p><strong>Please enter this code on the registration page to verify your email and complete your account setup.</strong></p>
         
         <div class='warning'>
    <strong>?? Security Notice:</strong>
     <ul style='margin: 5px 0; padding-left: 20px;'>
         <li>This code will expire in 10 minutes</li>
     <li>Never share this code with anyone</li>
        <li>Ace Job Agency will never ask for this code via phone or email</li>
                 <li>If you didn't request this code, please ignore this email</li>
     </ul>
</div>

            <p>If you didn't create an account with us, please ignore this email or contact support if you have concerns.</p>
   <p>Best regards,<br><strong>Ace Job Agency Team</strong></p>
      </div>
        <div class='footer'>
    <p>© 2025 Ace Job Agency. All rights reserved.</p>
          <p>This is an automated message, please do not reply to this email.</p>
        </div>
</div>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, htmlBody);
        }

        /// <summary>
  /// Send password reset email (or log to console)
        /// </summary>
        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetUrl)
        {
     if (_environment.IsDevelopment())
        {
       _logger.LogWarning("??????????????????????????????????????????????????");
      _logger.LogWarning("?    ?");
  _logger.LogWarning("?       ?? PASSWORD RESET LINK                ?");
   _logger.LogWarning("?   ?");
     _logger.LogWarning("??????????????????????????????????????????????????");
        _logger.LogWarning($"?  Email: {toEmail,-38} ?");
          _logger.LogWarning("?       ?");
 _logger.LogWarning($"?Click to reset: {resetUrl}");
     _logger.LogWarning("??");
    _logger.LogWarning("?  Valid for: 1 hour    ?");
   _logger.LogWarning("??????????????????????????????????????????????????");
         
     await Task.Delay(100);
return true;
        }

            var subject = "Password Reset Request - Ace Job Agency";
        
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
     .content {{ padding: 30px; background-color: #f8f9fa; }}
 .button {{ display: inline-block; padding: 12px 30px; background-color: #dc3545; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
      .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>?? Password Reset Request</h1>
        </div>
      <div class='content'>
            <p>Hello,</p>
  <p>We received a request to reset your password for your Ace Job Agency account.</p>
     <p>Click the button below to reset your password:</p>
            
    <div style='text-align: center;'>
           <a href='{resetUrl}' class='button'>Reset Password</a>
            </div>

       <p style='font-size: 12px; color: #666;'>Or copy and paste this link into your browser:</p>
      <p style='font-size: 12px; word-break: break-all;'>{resetUrl}</p>
         
         <div class='warning'>
           <strong>?? Security Notice:</strong>
   <ul style='margin: 5px 0; padding-left: 20px;'>
        <li>This link will expire in 1 hour</li>
                 <li>If you didn't request this reset, please ignore this email</li>
           <li>Your password has not been changed yet</li>
  </ul>
   </div>

    <p>Best regards,<br><strong>Ace Job Agency Team</strong></p>
        </div>
    <div class='footer'>
         <p>© 2025 Ace Job Agency. All rights reserved.</p>
            <p>This is an automated message, please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

      return await SendEmailAsync(toEmail, subject, htmlBody);
        }

        /// <summary>
        /// Send welcome email after successful registration
        /// </summary>
        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string userName)
    {
       if (_environment.IsDevelopment())
          {
       _logger.LogInformation("??????????????????????????????????????????????????");
                _logger.LogInformation("?          ?? WELCOME EMAIL   ?");
      _logger.LogInformation("??????????????????????????????????????????????????");
      _logger.LogInformation($"?  To: {toEmail,-41} ?");
    _logger.LogInformation($"?  Name: {userName,-39} ?");
             _logger.LogInformation("?  Status: Registration successful!     ?");
     _logger.LogInformation("??????????????????????????????????????????????????");
      
         await Task.Delay(100);
   return true;
            }

          var subject = "Welcome to Ace Job Agency!";
   
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #198754; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px; background-color: #f8f9fa; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
        .feature-box {{ background-color: white; border-left: 4px solid #198754; padding: 15px; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
 <h1>?? Welcome to Ace Job Agency!</h1>
        </div>
        <div class='content'>
            <p>Hello <strong>{userName}</strong>,</p>
            <p>Congratulations! Your account has been successfully created.</p>
   <p>You can now access all the features of Ace Job Agency:</p>
        
            <div class='feature-box'>
                <strong>? Secure Account</strong>
    <p style='margin: 5px 0 0 0; font-size: 14px;'>Your data is protected with enterprise-grade encryption</p>
</div>
            <div class='feature-box'>
          <strong>? Job Matching</strong>
            <p style='margin: 5px 0 0 0; font-size: 14px;'>Get matched with opportunities that fit your skills</p>
      </div>
   <div class='feature-box'>
    <strong>? Career Resources</strong>
  <p style='margin: 5px 0 0 0; font-size: 14px;'>Access exclusive career development tools</p>
          </div>

            <p style='margin-top: 20px;'>Start exploring opportunities today!</p>
    <p>Best regards,<br><strong>Ace Job Agency Team</strong></p>
        </div>
    <div class='footer'>
         <p>© 2025 Ace Job Agency. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

  return await SendEmailAsync(toEmail, subject, htmlBody);
        }
    }
}
