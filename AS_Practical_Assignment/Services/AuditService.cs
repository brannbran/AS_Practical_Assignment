using AS_Practical_Assignment.Data;
using AS_Practical_Assignment.Models;
using Microsoft.EntityFrameworkCore;

namespace AS_Practical_Assignment.Services
{
    public interface IAuditService
{
        Task LogAsync(string userId, string userEmail, string action, string status, 
     string? description = null, string? ipAddress = null, string? userAgent = null, 
string? additionalInfo = null);
        
     Task LogLoginAttemptAsync(string email, bool success, string? ipAddress, 
            string? userAgent, string? reason = null);
   
        Task LogLogoutAsync(string userId, string userEmail, string? ipAddress, string? userAgent);
        
        Task LogSessionExpiredAsync(string userId, string userEmail);
     
Task LogMultipleLoginAsync(string userId, string userEmail, string? ipAddress, string? userAgent);
        
        Task<List<AuditLog>> GetUserAuditLogsAsync(string userId, int count = 10);
        
        Task<List<AuditLog>> GetRecentAuditLogsAsync(int count = 50);
    }

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
   {
            _context = context;
    _logger = logger;
        }

      public async Task LogAsync(string userId, string userEmail, string action, string status,
            string? description = null, string? ipAddress = null, string? userAgent = null,
    string? additionalInfo = null)
        {
            try
    {
   var auditLog = new AuditLog
      {
       UserId = userId,
       UserEmail = userEmail,
        Action = action,
        Status = status,
     Description = description,
        IpAddress = ipAddress,
        UserAgent = userAgent,
  AdditionalInfo = additionalInfo,
  Timestamp = DateTime.UtcNow
        };

_context.AuditLogs.Add(auditLog);
          await _context.SaveChangesAsync();

     _logger.LogInformation("Audit Log: {Action} - {Status}", action, status);
            }
            catch (Exception ex)
    {
        _logger.LogError(ex, $"Failed to create audit log for action: {action}");
            }
        }

        public async Task LogLoginAttemptAsync(string email, bool success, string? ipAddress,
     string? userAgent, string? reason = null)
        {
         var action = success ? AuditActions.Login : AuditActions.LoginFailed;
            var status = success ? AuditStatus.Success : AuditStatus.Failed;
            var description = success 
      ? "User logged in successfully" 
         : $"Login failed: {reason ?? "Invalid credentials"}";

      await LogAsync(
                userId: email, // Use email as userId for failed attempts
    userEmail: email,
          action: action,
       status: status,
    description: description,
       ipAddress: ipAddress,
    userAgent: userAgent
            );
        }

     public async Task LogLogoutAsync(string userId, string userEmail, string? ipAddress, string? userAgent)
        {
   await LogAsync(
          userId: userId,
    userEmail: userEmail,
      action: AuditActions.Logout,
status: AuditStatus.Success,
    description: "User logged out",
      ipAddress: ipAddress,
      userAgent: userAgent
         );
        }

        public async Task LogSessionExpiredAsync(string userId, string userEmail)
        {
       await LogAsync(
        userId: userId,
                userEmail: userEmail,
       action: AuditActions.SessionExpired,
     status: AuditStatus.Warning,
          description: "Session expired due to inactivity"
    );
    }

        public async Task LogMultipleLoginAsync(string userId, string userEmail, string? ipAddress, string? userAgent)
    {
      await LogAsync(
       userId: userId,
       userEmail: userEmail,
     action: AuditActions.MultipleLogin,
           status: AuditStatus.Warning,
     description: "Multiple login detected - previous session invalidated",
            ipAddress: ipAddress,
                userAgent: userAgent
  );
}

        public async Task<List<AuditLog>> GetUserAuditLogsAsync(string userId, int count = 10)
  {
          return await _context.AuditLogs
            .Where(a => a.UserId == userId)
        .OrderByDescending(a => a.Timestamp)
        .Take(count)
     .ToListAsync();
        }

        public async Task<List<AuditLog>> GetRecentAuditLogsAsync(int count = 50)
        {
       return await _context.AuditLogs
         .OrderByDescending(a => a.Timestamp)
                .Take(count)
       .ToListAsync();
        }
    }
}
