using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AS_Practical_Assignment.Pages
{
    /// <summary>
    /// Test page for triggering different HTTP status codes
    /// WARNING: Remove or protect this page in production!
    /// </summary>
    public class TestErrorsModel : PageModel
    {
        private readonly ILogger<TestErrorsModel> _logger;

        public TestErrorsModel(ILogger<TestErrorsModel> logger)
        {
  _logger = logger;
        }

        public void OnGet()
        {
    _logger.LogInformation("Test Errors page accessed");
     }

        /// <summary>
        /// Trigger 401 Unauthorized
     /// </summary>
   public IActionResult OnPostTrigger401()
    {
 _logger.LogInformation("Triggering 401 Unauthorized error");
       
            // Set status code and redirect to error page
        Response.StatusCode = 401;
            return RedirectToPage("/Error", new { statusCode = 401 });
        }

        /// <summary>
        /// Trigger 403 Forbidden
        /// </summary>
        public IActionResult OnPostTrigger403()
        {
    _logger.LogInformation("Triggering 403 Forbidden error");
            
            Response.StatusCode = 403;
            return RedirectToPage("/Error", new { statusCode = 403 });
      }

        /// <summary>
        /// Trigger 404 Not Found
        /// </summary>
        public IActionResult OnPostTrigger404()
        {
            _logger.LogInformation("Triggering 404 Not Found error");
    
          Response.StatusCode = 404;
            return RedirectToPage("/Error", new { statusCode = 404 });
      }

        /// <summary>
/// Trigger 500 Internal Server Error
    /// Option 1: Redirect to error page
        /// Option 2: Actually throw an exception (commented out)
        /// </summary>
        public IActionResult OnPostTrigger500()
        {
     _logger.LogInformation("Triggering 500 Internal Server Error");
          
 // Option 1: Redirect to error page (safer for testing)
  Response.StatusCode = 500;
      return RedirectToPage("/Error", new { statusCode = 500 });
      
            // Option 2: Actually throw an exception to test exception handler
          // throw new Exception("Test exception - simulating 500 error");
        }

        /// <summary>
        /// Trigger 503 Service Unavailable
        /// </summary>
        public IActionResult OnPostTrigger503()
        {
     _logger.LogInformation("Triggering 503 Service Unavailable error");
            
   Response.StatusCode = 503;
return RedirectToPage("/Error", new { statusCode = 503 });
        }

     /// <summary>
 /// Trigger 429 Too Many Requests
   /// </summary>
        public IActionResult OnPostTrigger429()
 {
      _logger.LogInformation("Triggering 429 Too Many Requests error");
     
      Response.StatusCode = 429;
         return RedirectToPage("/Error", new { statusCode = 429 });
        }
  }
}
