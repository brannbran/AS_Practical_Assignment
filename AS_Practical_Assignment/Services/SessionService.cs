using AS_Practical_Assignment.Models;
using Microsoft.AspNetCore.Identity;

namespace AS_Practical_Assignment.Services
{
  public interface ISessionService
    {
        Task<string> CreateSecureSessionAsync(Member member, HttpContext httpContext);
Task<bool> ValidateSessionAsync(Member member, HttpContext httpContext);
   Task InvalidateSessionAsync(Member member);
        Task UpdateLastActivityAsync(Member member);
        bool IsSessionExpired(DateTime? lastActivity, int timeoutMinutes = 15);
        Task<bool> DetectMultipleLoginsAsync(Member member, string currentSessionId);
    }

    public class SessionService : ISessionService
    {
      private readonly UserManager<Member> _userManager;
    private readonly ILogger<SessionService> _logger;
private const int SESSION_TIMEOUT_MINUTES = 15;

        public SessionService(UserManager<Member> userManager, ILogger<SessionService> logger)
        {
            _userManager = userManager;
            _logger = logger;
   }

        public async Task<string> CreateSecureSessionAsync(Member member, HttpContext httpContext)
        {
            // Generate unique session ID
      var sessionId = Guid.NewGuid().ToString();

    // Get IP address and User Agent
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
       var userAgent = httpContext.Request.Headers["User-Agent"].ToString();

 // Update member with session info
        member.CurrentSessionId = sessionId;
member.LastLoginDate = DateTime.UtcNow;
  member.LastActivityDate = DateTime.UtcNow;
member.LastLoginIP = ipAddress;
      member.LastUserAgent = userAgent;

        await _userManager.UpdateAsync(member);

            // Store session ID in HTTP session
            httpContext.Session.SetString("SessionId", sessionId);
          httpContext.Session.SetString("UserId", member.Id);
      httpContext.Session.SetString("LoginTime", DateTime.UtcNow.ToString("o"));

   // Commit session to ensure it's saved
         await httpContext.Session.CommitAsync();

          _logger.LogInformation($"Secure session created for user {member.Email} from IP {ipAddress}. Session ID: {sessionId}");

            return sessionId;
        }

     public async Task<bool> ValidateSessionAsync(Member member, HttpContext httpContext)
 {
     // Ensure session is loaded
 await httpContext.Session.LoadAsync();
        
            // Get session ID from HTTP session
 var httpSessionId = httpContext.Session.GetString("SessionId");

    if (string.IsNullOrEmpty(httpSessionId))
    {
 _logger.LogWarning($"No session ID found in HTTP session for user {member.Email}");
        return false;
}

  // Compare with member's current session ID
       if (member.CurrentSessionId != httpSessionId)
     {
 _logger.LogWarning($"Session ID mismatch for user {member.Email}. HTTP Session: {httpSessionId}, DB Session: {member.CurrentSessionId}. Possible multiple login detected.");
         return false;
 }

     // Check session timeout
        if (IsSessionExpired(member.LastActivityDate, SESSION_TIMEOUT_MINUTES))
            {
     _logger.LogInformation($"Session expired for user {member.Email}");
return false;
      }

  return true;
        }

        public async Task InvalidateSessionAsync(Member member)
        {
            member.CurrentSessionId = null;
            await _userManager.UpdateAsync(member);
_logger.LogInformation($"Session invalidated for user {member.Email}");
        }

        public async Task UpdateLastActivityAsync(Member member)
{
          member.LastActivityDate = DateTime.UtcNow;
  await _userManager.UpdateAsync(member);
        }

        public bool IsSessionExpired(DateTime? lastActivity, int timeoutMinutes = 15)
        {
          if (!lastActivity.HasValue)
       return true;

       var expirationTime = lastActivity.Value.AddMinutes(timeoutMinutes);
            return DateTime.UtcNow > expirationTime;
  }

      public async Task<bool> DetectMultipleLoginsAsync(Member member, string currentSessionId)
        {
        // If member has a current session ID and it's different from the new one
            if (!string.IsNullOrEmpty(member.CurrentSessionId) && member.CurrentSessionId != currentSessionId)
            {
         _logger.LogWarning($"Multiple login detected for user {member.Email}");
     return true;
            }

    return false;
      }
    }
}
