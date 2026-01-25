using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AS_Practical_Assignment.Pages
{
  public class AccessDeniedModel : PageModel
    {
        private readonly ILogger<AccessDeniedModel> _logger;

        public AccessDeniedModel(ILogger<AccessDeniedModel> logger)
{
            _logger = logger;
        }

        public string? ReturnUrl { get; set; }

        public void OnGet(string? returnUrl = null)
        {
     ReturnUrl = returnUrl;
            var userId = User?.Identity?.Name ?? "Anonymous";
  _logger.LogWarning($"403 Access Denied: User '{userId}' attempted to access restricted resource. Return URL: {returnUrl}");
    }
    }
}
