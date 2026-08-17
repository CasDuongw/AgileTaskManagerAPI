using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace AgileTaskManager.Desktop
{
    // 2. Đổi gốc kế thừa từ ": Window" sang ": MetroWindow"
    public partial class MainWindow : Window
    {
        private readonly string ApiBaseUrl = "http://localhost:5279/api";
        private static readonly HttpClient client = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
        }

        
    }
}