using System.Net.Http;
using System.Net.Http.Headers;

namespace AgileTaskManager.Desktop
{
    public static class AppConfig
    {
        // Nơi duy nhất chứa URL của API. 
        public static readonly string ApiBaseUrl = "http://localhost:5279/api";
        
        // HttpClient dùng chung cho toàn bộ ứng dụng WPF
        public static readonly HttpClient Client = new HttpClient();

        // Hàm tiện ích để nhúng Token JWT vào Header
        public static void SetToken(string token)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
