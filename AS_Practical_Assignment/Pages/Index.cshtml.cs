using AS_Practical_Assignment.Models;
using AS_Practical_Assignment.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AS_Practical_Assignment.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly UserManager<Member> _userManager;
        private readonly IEncryptionService _encryptionService;

        public IndexModel(
            ILogger<IndexModel> logger,
            UserManager<Member> userManager,
            IEncryptionService encryptionService)
        {
            _logger = logger;
            _userManager = userManager;
            _encryptionService = encryptionService;
        }

        // Decrypted member data to display
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? NRIC { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? WhoAmI { get; set; }
        public string? Email { get; set; }
        public bool IsAuthenticated { get; set; }

        // Session information
        public DateTime? LastLoginDate { get; set; }
        public string? LastLoginIP { get; set; }

        public async Task OnGetAsync()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                IsAuthenticated = true;
                var user = await _userManager.GetUserAsync(User);

                if (user != null)
                {
                    // Decrypt customer data for display
                    FirstName = _encryptionService.Decrypt(user.EncryptedFirstName);
                    LastName = _encryptionService.Decrypt(user.EncryptedLastName);
                    NRIC = _encryptionService.Decrypt(user.EncryptedNRIC);
                    Email = user.Email;

                    // Decrypt and parse Date of Birth
                    var dobString = _encryptionService.Decrypt(user.EncryptedDateOfBirth);
                    if (DateTime.TryParse(dobString, out var dob))
                    {
                        DateOfBirth = dob;
                    }

                    // Decrypt Who Am I
                    if (!string.IsNullOrEmpty(user.EncryptedWhoAmI))
                    {
                        WhoAmI = _encryptionService.Decrypt(user.EncryptedWhoAmI);
                    }

                    // Session information
                    LastLoginDate = user.LastLoginDate;
                    LastLoginIP = user.LastLoginIP;
                }
            }
        }
    }
}
