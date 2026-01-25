using Microsoft.AspNetCore.DataProtection;

namespace AS_Practical_Assignment.Services
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string encryptedText);
    }

    public class EncryptionService : IEncryptionService
    {
        private readonly IDataProtector _protector;

        public EncryptionService(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("AceJobAgency.DataProtection");
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            return _protector.Protect(plainText);
        }

        public string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;

            try
            {
                return _protector.Unprotect(encryptedText);
            }
            catch
            {
                // If decryption fails, return empty string
                // In production, you should log this error
                return string.Empty;
            }
        }
    }
}
