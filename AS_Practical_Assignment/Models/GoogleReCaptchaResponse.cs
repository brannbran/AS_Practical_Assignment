using System.Text.Json.Serialization;

namespace AS_Practical_Assignment.Models
{
    public class GoogleReCaptchaResponse
    {
  [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("challenge_ts")]
   public DateTime ChallengeTimestamp { get; set; }

  [JsonPropertyName("hostname")]
        public string Hostname { get; set; } = string.Empty;

        [JsonPropertyName("score")]
   public double Score { get; set; }

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

    [JsonPropertyName("error-codes")]
        public List<string> ErrorCodes { get; set; } = new List<string>();
    }

    public class ReCaptchaValidationResult
    {
        public bool IsValid { get; set; }
      public double Score { get; set; }
        public string Action { get; set; } = string.Empty;
      public List<string> Errors { get; set; } = new List<string>();
        public string Message { get; set; } = string.Empty;
    }
}
