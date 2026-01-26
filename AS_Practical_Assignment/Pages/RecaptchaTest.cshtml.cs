using AS_Practical_Assignment.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using AS_Practical_Assignment.Models;

namespace AS_Practical_Assignment.Pages
{
    public class RecaptchaTestModel : PageModel
    {
   private readonly IReCaptchaService _reCaptchaService;
    private readonly IOptions<GoogleReCaptchaConfig> _config;
   private readonly IConfiguration _configuration;

  public RecaptchaTestModel(
      IReCaptchaService reCaptchaService,
 IOptions<GoogleReCaptchaConfig> config,
       IConfiguration configuration)
 {
    _reCaptchaService = reCaptchaService;
   _config = config;
   _configuration = configuration;
  }

   public string SiteKey { get; set; } = string.Empty;
  public string ConfigSource { get; set; } = string.Empty;

   public void OnGet()
    {
    // Get site key from service
  SiteKey = _reCaptchaService.GetSiteKey();

       // Get configuration details for debugging
   var configSiteKey = _configuration["GoogleReCaptcha:SiteKey"];
    var configSecretKey = _configuration["GoogleReCaptcha:SecretKey"];
      var optionsSiteKey = _config.Value.SiteKey;

     ConfigSource = $@"
IConfiguration SiteKey: {configSiteKey ?? "NULL"}
     IOptions SiteKey: {optionsSiteKey ?? "NULL"}
Service GetSiteKey(): {SiteKey ?? "NULL"}
    ";
  }
    }
}
