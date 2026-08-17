using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AgileTaskManagerAPI.Model
{
    // 1. Chỉ định cột Email là duy nhất (Unique)
    [Index(nameof(Email), IsUnique = true)]
    public class User
    {
        public int UserId { get; set; } // Khóa chính
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        // 2. Giới hạn độ dài để EF Core không tạo kiểu MAX dưới SQL
        [MaxLength(100)]
        public string Email { get; set; }
    }
}
