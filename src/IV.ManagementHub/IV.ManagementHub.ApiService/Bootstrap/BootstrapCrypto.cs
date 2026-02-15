using System.Security.Cryptography;
using System.Text;

namespace IV.ManagementHub.ApiService.Bootstrap
{
    public static class BootstrapCrypto
    {
        private const int SaltSizeBytes = 16;
        private const int HashSizeBytes = 32;
        private const int Iterations = 100_000;

        public static string CreateSalt()
        {
            Span<byte> salt = stackalloc byte[SaltSizeBytes];
            RandomNumberGenerator.Fill(salt);
            return Convert.ToBase64String(salt);
        }

        public static string HashPassword(string password, string salt)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password is required.", nameof(password));
            }

            if (string.IsNullOrWhiteSpace(salt))
            {
                throw new ArgumentException("Password salt is required.", nameof(salt));
            }

            var saltBytes = Convert.FromBase64String(salt);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                saltBytes,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSizeBytes);

            return Convert.ToBase64String(hashBytes);
        }

        public static bool VerifyPassword(string password, string salt, string expectedHash)
        {
            if (string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(salt) ||
                string.IsNullOrWhiteSpace(expectedHash))
            {
                return false;
            }

            var calculatedHash = HashPassword(password, salt);
            var calculatedBytes = Convert.FromBase64String(calculatedHash);
            var expectedBytes = Convert.FromBase64String(expectedHash);

            return CryptographicOperations.FixedTimeEquals(calculatedBytes, expectedBytes);
        }
    }
}
