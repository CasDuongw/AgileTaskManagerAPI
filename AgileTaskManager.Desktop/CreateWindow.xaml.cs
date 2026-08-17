using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace AgileTaskManager.Desktop
{
    public partial class CreateWindow : Window
    {
        // Nhớ kiểm tra lại Port của bạn nếu nó thay đổi nhé
        private readonly string ApiBaseUrl = "http://localhost:5279/api";
        private static readonly HttpClient client = new HttpClient();

        public CreateWindow()
        {
            InitializeComponent();
        }

        private async void BtnCreateUser_Click(object sender, RoutedEventArgs e)
        {
            var user = new { username = txtUserName.Text, email = txtEmail.Text, passwordHash = txtPassword.Password };
            var response = await client.PostAsJsonAsync($"{ApiBaseUrl}/Users/register", user);

            if (response.IsSuccessStatusCode) lblUserResult.Text = "✅ Đã tạo User thành công!";
            else lblUserResult.Text = "❌ Lỗi: " + await response.Content.ReadAsStringAsync();
        }

        private async void BtnCreateProject_Click(object sender, RoutedEventArgs e)
        {
            var project = new { projectName = txtProjectName.Text, ownerId = int.Parse(txtOwnerId.Text) };
            var response = await client.PostAsJsonAsync($"{ApiBaseUrl}/Projects", project);

            if (response.IsSuccessStatusCode) lblProjectResult.Text = "✅ Đã tạo Dự án thành công!";
            else lblProjectResult.Text = "❌ Lỗi: Kiểm tra lại ID User.";
        }

        private async void BtnCreateTask_Click(object sender, RoutedEventArgs e)
        {
            var task = new { taskName = txtTaskName.Text, projectId = int.Parse(txtProjectId.Text) };
            var response = await client.PostAsJsonAsync($"{ApiBaseUrl}/Tasks", task);

            if (response.IsSuccessStatusCode) lblTaskResult.Text = "✅ Đã tạo Task thành công!";
            else lblTaskResult.Text = "❌ Lỗi: Kiểm tra lại ID Dự án.";
        }
    }
}