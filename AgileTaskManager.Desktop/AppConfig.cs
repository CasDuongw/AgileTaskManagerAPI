using System.Net.Http;
using System.Net.Http.Headers;

namespace AgileTaskManager.Desktop
{
    public static class AppConfig
    {
        // Nơi duy nhất chứa URL của API. 
        public static readonly string ApiBaseUrl = "https://agiletaskmanagerapi.onrender.com/api";
        
        // HttpClient dùng chung cho toàn bộ ứng dụng WPF
        public static readonly HttpClient Client = new HttpClient();

        // Lưu trữ Username của người dùng đang đăng nhập
        public static string CurrentUsername { get; set; } = string.Empty;

        // Hàm tiện ích để nhúng Token JWT vào Header
        public static void SetToken(string token)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
