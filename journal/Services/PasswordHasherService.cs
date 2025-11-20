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
                Console.WriteLine($"=== ПРОВЕРКА ПАРОЛЯ ===");
                Console.WriteLine($"Введенный пароль: '{password}'");
                Console.WriteLine($"Длина пароля: {password.Length}");
                Console.WriteLine($"Хеш из БД: '{hashedPassword}'");
                Console.WriteLine($"Соль из БД: '{salt}'");
                Console.WriteLine($"Длина соли: {salt.Length}");

                byte[] saltBytes = Convert.FromBase64String(salt);
                Console.WriteLine($"Соль в байтах: {BitConverter.ToString(saltBytes)}");

                string computedHash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                    password: password,
                    salt: saltBytes,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 100000,
                    numBytesRequested: 256 / 8));

                Console.WriteLine($"Вычисленный хеш: '{computedHash}'");
                Console.WriteLine($"Длина вычисленного хеша: {computedHash.Length}");
                Console.WriteLine($"Длина хеша из БД: {hashedPassword.Length}");

                bool result = computedHash == hashedPassword;
                Console.WriteLine($"Результат сравнения: {result}");
                Console.WriteLine($"=== КОНЕЦ ПРОВЕРКИ ===\n");

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в VerifyPassword: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }
    }
}