using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AS_Practical_Assignment.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string ErrorTitle { get; set; } = string.Empty;

        private readonly ILogger<ErrorModel> _logger;

        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }

        public void OnGet(int? statusCode = null)
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            StatusCode = statusCode ?? HttpContext.Response.StatusCode;

            // Set appropriate error messages based on status code
            SetErrorDetails(StatusCode);

            // Log the error
            _logger.LogWarning($"Error page accessed. Status Code: {StatusCode}, Request ID: {RequestId}");
        }

        private void SetErrorDetails(int statusCode)
        {
            switch (statusCode)
            {
                case 400:
                    ErrorTitle = "Bad Request";
                    ErrorMessage = "The request could not be understood by the server. Please check your input and try again.";
                    break;
                case 401:
                    ErrorTitle = "Unauthorized";
                    ErrorMessage = "You are not authorized to access this resource. Please log in and try again.";
                    break;
                case 403:
                    ErrorTitle = "Forbidden";
                    ErrorMessage = "You don't have permission to access this resource. This page requires authentication or specific permissions.";
                    break;
                case 404:
                    ErrorTitle = "Page Not Found";
                    ErrorMessage = "The page you are looking for might have been removed, had its name changed, or is temporarily unavailable.";
                    break;
                case 405:
                    ErrorTitle = "Method Not Allowed";
                    ErrorMessage = "The request method is not supported for this resource.";
                    break;
                case 408:
                    ErrorTitle = "Request Timeout";
                    ErrorMessage = "The server timed out waiting for the request. Please try again.";
                    break;
                case 429:
                    ErrorTitle = "Too Many Requests";
                    ErrorMessage = "You have sent too many requests in a given amount of time. Please try again later.";
                    break;
                case 500:
                    ErrorTitle = "Internal Server Error";
                    ErrorMessage = "An unexpected error occurred on the server. Our team has been notified and is working to fix the issue.";
                    break;
                case 502:
                    ErrorTitle = "Bad Gateway";
                    ErrorMessage = "The server received an invalid response. Please try again later.";
                    break;
                case 503:
                    ErrorTitle = "Service Unavailable";
                    ErrorMessage = "The service is temporarily unavailable due to maintenance or high load. Please try again in a few minutes.";
                    break;
                case 504:
                    ErrorTitle = "Gateway Timeout";
                    ErrorMessage = "The server did not receive a timely response. Please try again later.";
                    break;
                default:
                    ErrorTitle = "Error";
                    ErrorMessage = "An unexpected error occurred while processing your request.";
                    break;
            }
        }
    }
}
