using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace AS_Practical_Assignment.Middleware
{
    /// <summary>
    /// Middleware for testing HTTP error codes
    /// Enable in Program.cs with app.UseErrorTesting()
    /// WARNING: Only use in development!
    /// </summary>
    public class ErrorTestingMiddleware
    {
      private readonly RequestDelegate _next;
        private readonly ILogger<ErrorTestingMiddleware> _logger;

 public ErrorTestingMiddleware(RequestDelegate next, ILogger<ErrorTestingMiddleware> logger)
        {
            _next = next;
   _logger = logger;
   }

        public async Task InvokeAsync(HttpContext context)
  {
      var path = context.Request.Path.Value?.ToLower();

            // Special test routes that trigger specific errors
      if (path != null)
    {
              // Test 401 Unauthorized
       if (path == "/test/401" || path == "/test/unauthorized")
        {
      _logger.LogWarning("Test route triggered: 401 Unauthorized");
       context.Response.StatusCode = 401;
       await context.Response.WriteAsync("401 Unauthorized - Test Route");
                    return;
          }

             // Test 403 Forbidden
       if (path == "/test/403" || path == "/test/forbidden")
         {
     _logger.LogWarning("Test route triggered: 403 Forbidden");
      context.Response.StatusCode = 403;
           await context.Response.WriteAsync("403 Forbidden - Test Route");
         return;
       }

     // Test 404 Not Found
       if (path == "/test/404" || path == "/test/notfound")
  {
      _logger.LogWarning("Test route triggered: 404 Not Found");
     context.Response.StatusCode = 404;
await context.Response.WriteAsync("404 Not Found - Test Route");
     return;
    }

          // Test 500 Internal Server Error
     if (path == "/test/500" || path == "/test/servererror")
   {
      _logger.LogError("Test route triggered: 500 Internal Server Error");
   
       // Option 1: Set status code
        // context.Response.StatusCode = 500;
  // await context.Response.WriteAsync("500 Internal Server Error - Test Route");
        
               // Option 2: Throw exception (tests exception handler)
        throw new Exception("Test exception - simulating 500 error");
  }

                // Test 503 Service Unavailable
       if (path == "/test/503" || path == "/test/unavailable")
       {
       _logger.LogWarning("Test route triggered: 503 Service Unavailable");
         context.Response.StatusCode = 503;
         await context.Response.WriteAsync("503 Service Unavailable - Test Route");
      return;
  }

      // Test 429 Too Many Requests
         if (path == "/test/429" || path == "/test/ratelimit")
        {
        _logger.LogWarning("Test route triggered: 429 Too Many Requests");
    context.Response.StatusCode = 429;
   await context.Response.WriteAsync("429 Too Many Requests - Test Route");
         return;
          }
}

    // Continue to next middleware
  await _next(context);
        }
    }

    public static class ErrorTestingMiddlewareExtensions
    {
        /// <summary>
        /// Add error testing middleware (DEVELOPMENT ONLY!)
        /// </summary>
     public static IApplicationBuilder UseErrorTesting(this IApplicationBuilder builder)
        {
       return builder.UseMiddleware<ErrorTestingMiddleware>();
        }
    }
}
