using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace AgileTaskManager.Desktop
{
    // 2. Đổi gốc kế thừa từ ": Window" sang ": MetroWindow"
    public partial class MainWindow : Window
    {
        // Lấy URL cấu hình từ AppConfig
        // Đã xóa biến local ApiBaseUrl hardcode
        private static readonly HttpClient client = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
        }

        
    }
}