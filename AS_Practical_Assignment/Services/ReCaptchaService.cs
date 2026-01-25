using AS_Practical_Assignment.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AS_Practical_Assignment.Services
{
    public interface IReCaptchaService
    {
     Task<ReCaptchaValidationResult> ValidateAsync(string token, string action, string? remoteIp = null);
     string GetSiteKey();
    }

    public class ReCaptchaService : IReCaptchaService
 {
        private readonly GoogleReCaptchaConfig _config;
 private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ReCaptchaService> _logger;
        private const string RECAPTCHA_VERIFY_URL = "https://www.google.com/recaptcha/api/siteverify";

        public ReCaptchaService(
            IOptions<GoogleReCaptchaConfig> config,
            IHttpClientFactory httpClientFactory,
            ILogger<ReCaptchaService> logger)
        {
            _config = config.Value;
        _httpClientFactory = httpClientFactory;
            _logger = logger;
      }

        public string GetSiteKey()
    {
       return _config.SiteKey;
        }

        public async Task<ReCaptchaValidationResult> ValidateAsync(string token, string action, string? remoteIp = null)
        {
  var result = new ReCaptchaValidationResult
        {
         Action = action
     };

            try
     {
                if (string.IsNullOrEmpty(token))
        {
      result.IsValid = false;
  result.Message = "reCAPTCHA token is missing";
         result.Errors.Add("missing-input-response");
          _logger.LogWarning("reCAPTCHA validation failed: Token is missing");
     return result;
      }

    // Build request parameters
    var requestParams = new Dictionary<string, string>
            {
  { "secret", _config.SecretKey },
     { "response", token }
    };

        if (!string.IsNullOrEmpty(remoteIp))
        {
         requestParams.Add("remoteip", remoteIp);
    }

       // Send verification request to Google
           var client = _httpClientFactory.CreateClient();
     var response = await client.PostAsync(
      RECAPTCHA_VERIFY_URL,
       new FormUrlEncodedContent(requestParams));

      response.EnsureSuccessStatusCode();

           var jsonResponse = await response.Content.ReadAsStringAsync();
       var reCaptchaResponse = JsonSerializer.Deserialize<GoogleReCaptchaResponse>(jsonResponse);

       if (reCaptchaResponse == null)
            {
          result.IsValid = false;
         result.Message = "Failed to parse reCAPTCHA response";
              _logger.LogError("Failed to deserialize reCAPTCHA response");
        return result;
          }

    // Check if verification was successful
         if (!reCaptchaResponse.Success)
   {
     result.IsValid = false;
              result.Errors = reCaptchaResponse.ErrorCodes;
         result.Message = $"reCAPTCHA verification failed: {string.Join(", ", reCaptchaResponse.ErrorCodes)}";
             _logger.LogWarning($"reCAPTCHA verification failed: {string.Join(", ", reCaptchaResponse.ErrorCodes)}");
     return result;
           }

    // Validate action matches
              if (!string.IsNullOrEmpty(action) && reCaptchaResponse.Action != action)
      {
   result.IsValid = false;
  result.Message = $"Action mismatch. Expected: {action}, Got: {reCaptchaResponse.Action}";
 _logger.LogWarning($"reCAPTCHA action mismatch. Expected: {action}, Got: {reCaptchaResponse.Action}");
 return result;
     }

    // Validate score (for reCAPTCHA v3)
      result.Score = reCaptchaResponse.Score;
     if (reCaptchaResponse.Score < _config.MinimumScore)
    {
        result.IsValid = false;
     result.Message = $"reCAPTCHA score too low: {reCaptchaResponse.Score} (minimum: {_config.MinimumScore})";
                _logger.LogWarning($"reCAPTCHA score too low: {reCaptchaResponse.Score} for action: {action}");
       return result;
             }

            // All checks passed
       result.IsValid = true;
       result.Message = "reCAPTCHA validation successful";
         _logger.LogInformation($"reCAPTCHA validation successful. Score: {reCaptchaResponse.Score}, Action: {action}");

    return result;
            }
            catch (HttpRequestException ex)
      {
           result.IsValid = false;
      result.Message = "Failed to connect to reCAPTCHA service";
    _logger.LogError(ex, "HTTP error during reCAPTCHA validation");
           return result;
     }
            catch (Exception ex)
            {
         result.IsValid = false;
        result.Message = "An error occurred during reCAPTCHA validation";
     _logger.LogError(ex, "Unexpected error during reCAPTCHA validation");
      return result;
            }
        }
    }
}
