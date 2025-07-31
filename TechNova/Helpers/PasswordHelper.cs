using System.Security.Cryptography;
using System.Text;

namespace TechNova.Helpers
{
    public class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (var b in bytes)
                    builder.Append(b.ToString("x2")); // Convert to hex string
                return builder.ToString();
            }
        }
    }
}
