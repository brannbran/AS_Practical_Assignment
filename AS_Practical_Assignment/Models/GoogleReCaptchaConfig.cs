namespace AS_Practical_Assignment.Models
{
    public class GoogleReCaptchaConfig
    {
        public string SiteKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
      public string Version { get; set; } = "v3";
    public double MinimumScore { get; set; } = 0.5;
    }
}
