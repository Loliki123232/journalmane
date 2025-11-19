using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using System.Text;

namespace journal.Services
{
    public class PasswordHasherService
    {
        public (string Hash, string Salt) HashPassword(string password)
        {
            // Генерируем случайную соль
            byte[] saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            string salt = Convert.ToBase64String(saltBytes);

            // Хешируем пароль с солью
            string hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            return (Hash: hash, Salt: salt);
        }

        public bool VerifyPassword(string password, string hashedPassword, string salt)
        {
            try
            {
                byte[] saltBytes = Convert.FromBase64String(salt);

                string computedHash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                    password: password,
                    salt: saltBytes,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 100000,
                    numBytesRequested: 256 / 8));

                return computedHash == hashedPassword;
            }
            catch
            {
                return false;
            }
        }
    }
}