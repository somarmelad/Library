using BCrypt.Net;

namespace Library2.Helpers
{
    public static class PasswordHasher
    {
        // Хеширует пароль
        public static string HashPassword(string password)
        {
            // Используем BCrypt для надежного хеширования
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Проверяет пароль
        public static bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
    }
}