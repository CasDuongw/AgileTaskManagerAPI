namespace AgileTaskManagerAPI.Model
{
    public class User
    {
        public int UserId { get; set; } // Khóa chính
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
