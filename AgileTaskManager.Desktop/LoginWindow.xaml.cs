using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace AgileTaskManager.Desktop
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        public class LoginResponse
        {
            public string token { get; set; }
            public int userId { get; set; }
            public string username { get; set; }
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowMessage("Vui lòng nhập đầy đủ Email và Mật khẩu!");
                return;
            }

            var requestData = new
            {
                email = email,
                password = password
            };

            try
            {
                var response = await AppConfig.Client.PostAsJsonAsync($"{AppConfig.ApiBaseUrl}/Auth/login", requestData);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    if (result != null && !string.IsNullOrEmpty(result.token))
                    {
                        // Lưu Token vào HttpClient tĩnh
                        AppConfig.SetToken(result.token);

                        // Mở màn hình chính
                        DashboardWindow dashboard = new DashboardWindow();
                        dashboard.Show();

                        // Đóng màn hình đăng nhập
                        this.Close();
                    }
                }
                else
                {
                    ShowMessage("Đăng nhập thất bại. Vui lòng kiểm tra lại thông tin!");
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Lỗi kết nối Server: {ex.Message}");
            }
        }

        private void ShowMessage(string msg)
        {
            lblMessage.Text = msg;
            lblMessage.Visibility = Visibility.Visible;
        }
    }
}
