using System.Security.Cryptography;
using System.Text;

namespace TechNova.Helpers
{
    // SHA-256 hex hashing (legacy) with constant-time verification
    public static class PasswordHelper
    {
        // Hash a plain password -> lowercase hex SHA-256 (no salt)
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password ?? ""));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString(); // e.g., "e3b0c44298fc1c14..."
        }

        // Verify plain password against stored SHA-256 hex hash (constant-time)
        public static bool VerifyPassword(string password, string storedHashHex)
        {
            if (string.IsNullOrWhiteSpace(storedHashHex)) return false;

            // recompute hash of the provided password
            string candidate = HashPassword(password);

            // constant-time compare (avoid timing leaks)
            if (candidate.Length != storedHashHex.Length) return false;

            int diff = 0;
            for (int i = 0; i < candidate.Length; i++)
                diff |= candidate[i] ^ ToLowerHexChar(storedHashHex[i]);

            return diff == 0;

            static char ToLowerHexChar(char c)
            {
                // normalize to lowercase without allocations
                if (c >= 'A' && c <= 'F') return (char)(c + 32);
                return c;
            }
        }
    }
}
