using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AS_Practical_Assignment.Services;

namespace AS_Practical_Assignment.Pages
{
    public class TestEmailModel : PageModel
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<TestEmailModel> _logger;

     public TestEmailModel(IEmailService emailService, ILogger<TestEmailModel> logger)
  {
       _emailService = emailService;
 _logger = logger;
    }

[BindProperty]
    public string TestEmail { get; set; } = "";

        public string Message { get; set; } = "";
      public bool IsSuccess { get; set; }
      public string ErrorDetails { get; set; } = "";

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
  if (string.IsNullOrWhiteSpace(TestEmail))
            {
        Message = "Please enter an email address";
  IsSuccess = false;
       return Page();
    }

   try
            {
     _logger.LogInformation($"Testing email send to {TestEmail}");
            
          var result = await _emailService.SendOtpEmailAsync(TestEmail, "123456", "Test User");
    
       if (result)
           {
       Message = $"? SUCCESS! Email sent to {TestEmail}. Check your inbox!";
               IsSuccess = true;
       }
    else
          {
     Message = $"? FAILED to send email to {TestEmail}. Check Output window for details.";
          IsSuccess = false;
            }
       }
   catch (Exception ex)
            {
    Message = $"? ERROR: {ex.Message}";
          ErrorDetails = ex.ToString();
   IsSuccess = false;
         _logger.LogError(ex, $"Test email failed for {TestEmail}");
   }

   return Page();
        }
    }
}
