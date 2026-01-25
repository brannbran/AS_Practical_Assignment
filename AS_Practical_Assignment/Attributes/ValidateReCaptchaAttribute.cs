using AS_Practical_Assignment.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AS_Practical_Assignment.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ValidateReCaptchaAttribute : ActionFilterAttribute
    {
        private readonly string _action;

   public ValidateReCaptchaAttribute(string action)
        {
      _action = action;
 }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
 {
   var reCaptchaService = context.HttpContext.RequestServices.GetService<IReCaptchaService>();
       var auditService = context.HttpContext.RequestServices.GetService<IAuditService>();

            if (reCaptchaService == null)
      {
          await next();
return;
  }

            // Get reCAPTCHA token from form
       var token = context.HttpContext.Request.Form["g-recaptcha-response"].ToString();
  var remoteIp = context.HttpContext.Connection.RemoteIpAddress?.ToString();

      // Validate token
       var validationResult = await reCaptchaService.ValidateAsync(token, _action, remoteIp);

   if (!validationResult.IsValid)
    {
    // Log failed reCAPTCHA validation
    if (auditService != null)
    {
      var userEmail = context.HttpContext.User?.Identity?.Name ?? "Unknown";
         await auditService.LogAsync(
             userEmail,
  userEmail,
      "reCAPTCHA Failed",
     "Failed",
         $"reCAPTCHA validation failed for action: {_action}. {validationResult.Message}",
      remoteIp,
       context.HttpContext.Request.Headers["User-Agent"].ToString(),
         $"Score: {validationResult.Score}"
    );
     }

    // Add error to ModelState
        if (context.Controller is Controller controller)
     {
    controller.ModelState.AddModelError(string.Empty,
          "Bot detection failed. Please try again or contact support if you believe this is an error.");
         }
  else if (context.Controller is ControllerBase controllerBase)
  {
   controllerBase.ModelState.AddModelError(string.Empty,
   "Bot detection failed. Please try again or contact support if you believe this is an error.");
    }

          // For Razor Pages
     var pageModel = context.Controller as Microsoft.AspNetCore.Mvc.RazorPages.PageModel;
    if (pageModel != null)
       {
   pageModel.ModelState.AddModelError(string.Empty,
  "Bot detection failed. Please try again or contact support if you believe this is an error.");
      }

    context.Result = new BadRequestObjectResult(context.ModelState);
    return;
  }

       // Log successful reCAPTCHA validation
         if (auditService != null)
       {
     var userEmail = context.HttpContext.User?.Identity?.Name ?? "Unknown";
        await auditService.LogAsync(
     userEmail,
       userEmail,
      "reCAPTCHA Passed",
       "Success",
                $"reCAPTCHA validation successful for action: {_action}",
  remoteIp,
          context.HttpContext.Request.Headers["User-Agent"].ToString(),
    $"Score: {validationResult.Score}"
   );
   }

            await next();
        }
    }
}
