using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

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
        private readonly ILogger<EncryptionService> _logger;

        public EncryptionService(IDataProtectionProvider provider, ILogger<EncryptionService> logger)
        {
            _protector = provider.CreateProtector("AceJobAgency.DataProtection");
            _logger = logger;
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            try
            {
                return _protector.Protect(plainText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Encryption failed for data");
                throw new InvalidOperationException("Failed to encrypt sensitive data", ex);
            }
        }

        public string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;

            try
            {
                return _protector.Unprotect(encryptedText);
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Decryption failed. This may indicate data tampering or corruption.");
                throw new InvalidOperationException("Failed to decrypt sensitive data. The data may be corrupted or tampered.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during decryption");
                throw new InvalidOperationException("Failed to decrypt sensitive data", ex);
            }
        }
    }
}
