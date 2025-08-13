using System.Security.Cryptography;
using System.Text;

namespace TechNova.Helpers
{
    // helper to hash passwords with SHA-256 (returns hex string)
    public class PasswordHelper
    {
        // create SHA-256 hash of the given password
        public static string HashPassword(string password)
        {
            // make a SHA256 object
            using (var sha256 = SHA256.Create())
            {
                // get hash bytes from the UTF-8 password
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                // build a hex string from the bytes
                StringBuilder builder = new StringBuilder();
                // convert each byte to 2-digit lowercase hex
                foreach (var b in bytes)
                    builder.Append(b.ToString("x2"));
                // return the final hex string
                return builder.ToString();
            }
        }
    }
}
