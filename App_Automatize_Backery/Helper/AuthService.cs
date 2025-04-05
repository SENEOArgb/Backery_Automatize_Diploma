using App_Automatize_Backery.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace App_Automatize_Backery.Helper
{
    internal class AuthService
    {
        private readonly MinBakeryDbContext _context;

        public AuthService(MinBakeryDbContext context)
        {
            _context = context;
        }

        public User Authenticate(string login, string password)
        {
            byte[] passwordHash = ComputeSha256Hash(password);

            var user = _context.Users
                .Include(u => u.UserRole) // Принудительно загружаем UserRole
                .FirstOrDefault(u => u.UserLogin == login);

            if (user == null)
            {
                MessageBox.Show("Пользователь не найден!");
                return null;
            }

            // Конвертируем хэш из БД обратно в byte[], если UserHashPassword хранится как varbinary
            byte[] storedHash = user.UserHashPassword;

            // Сравниваем массивы байтов напрямую
            if (!passwordHash.SequenceEqual(storedHash))
            {
                MessageBox.Show("Пароль не совпадает!");
                return null;
            }

            return new User
            {
                UserId = user.UserId,
                UserName = user.UserName,
                UserSurname = user.UserSurname,
                UserLogin = user.UserLogin,
                UserHashPassword = user.UserHashPassword,
                UserRoleId = user.UserRoleId,
                UserRoleName = user.UserRole.UserRoleName
            };
        }

        private byte[] ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                return sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            }
        }
    }
}
