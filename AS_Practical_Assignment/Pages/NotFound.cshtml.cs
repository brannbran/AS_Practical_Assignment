using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AS_Practical_Assignment.Pages
{
    public class NotFoundModel : PageModel
    {
        private readonly ILogger<NotFoundModel> _logger;

        public NotFoundModel(ILogger<NotFoundModel> logger)
        {
        _logger = logger;
        }

        public string? RequestedPath { get; set; }

        public void OnGet()
        {
            RequestedPath = HttpContext.Request.Path;
            var sanitisedPath = RequestedPath?.Replace("\n", string.Empty).Replace("\r", string.Empty);
            _logger.LogWarning($"404 Not Found: User attempted to access non-existent path: {sanitisedPath}");
        }
    }
}
