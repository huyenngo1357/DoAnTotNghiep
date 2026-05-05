using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HappyHouse.Models
{
    public class Common
    {
        // BCrypt - hash mật khẩu
        public static string HashPassword(string pwd)
        {
            return BCrypt.Net.BCrypt.HashPassword(pwd);
        }

        // BCrypt - kiểm tra mật khẩu khớp với hash
        public static bool VerifyPassword(string pwd, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(pwd, hash);
        }
    }
}