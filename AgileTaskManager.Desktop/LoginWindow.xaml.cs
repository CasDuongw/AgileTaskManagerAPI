using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace AgileTaskManager.Desktop
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            PreWarmServer();
        }

        private void PreWarmServer()
        {
            // Đánh thức trước máy chủ Render trong lúc người dùng đang gõ email & pass
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    await AppConfig.Client.GetAsync($"{AppConfig.ApiBaseUrl}/health");
                }
                catch
                {
                    // Bỏ qua lỗi pre-warm
                }
            });
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

            lblMessage.Visibility = Visibility.Collapsed;

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

            // Bật trạng thái Loading
            btnLogin.IsEnabled = false;
            btnLogin.Content = "ĐANG XỬ LÝ...";
            pbLoading.Visibility = Visibility.Visible;
            lblStatus.Text = "Đang kết nối đến máy chủ đám mây...";
            lblStatus.Visibility = Visibility.Visible;
            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            // Timer thông báo nếu Render đang ngủ (Cold Start)
            using var cts = new System.Threading.CancellationTokenSource();
            _ = System.Threading.Tasks.Task.Delay(3000, cts.Token).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (pbLoading.Visibility == Visibility.Visible)
                    {
                        lblStatus.Text = "Máy chủ đang thức dậy (Render Cold Start), vui lòng chờ trong giây lát...";
                    }
                });
            }, cts.Token);

            try
            {
                var response = await AppConfig.Client.PostAsJsonAsync($"{AppConfig.ApiBaseUrl}/Auth/login", requestData);
                cts.Cancel();

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    if (result != null && !string.IsNullOrEmpty(result.token))
                    {
                        // Lưu Token vào HttpClient tĩnh
                        AppConfig.SetToken(result.token);
                        AppConfig.CurrentUsername = result.username;

                        // Mở màn hình chính
                        DashboardWindow dashboard = App.ServiceProvider.GetRequiredService<DashboardWindow>();
                        dashboard.Show();

                        // Đóng màn hình đăng nhập
                        this.Close();
                        return;
                    }
                }
                else
                {
                    ShowMessage("Đăng nhập thất bại. Vui lòng kiểm tra lại thông tin!");
                }
            }
            catch (Exception ex)
            {
                cts.Cancel();
                ShowMessage($"Lỗi kết nối Server: {ex.Message}");
            }
            finally
            {
                cts.Cancel();
                btnLogin.IsEnabled = true;
                btnLogin.Content = "ĐĂNG NHẬP";
                pbLoading.Visibility = Visibility.Collapsed;
                lblStatus.Visibility = Visibility.Collapsed;
                System.Windows.Input.Mouse.OverrideCursor = null;
            }
        }

        private void ShowMessage(string msg)
        {
            lblMessage.Text = msg;
            lblMessage.Visibility = Visibility.Visible;
        }
    }
}

