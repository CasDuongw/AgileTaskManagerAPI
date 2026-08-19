using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AgileTaskManager.Desktop
{
    public partial class DashboardWindow : Window
    {
        // Lấy URL cấu hình từ AppConfig
        // Đã xóa biến local ApiBaseUrl hardcode
        private static readonly HttpClient client = new HttpClient();
        private bool _isLoadingProjects;

        public int SelectedProjectId =>
            cboProjects.SelectedValue is int id ? id : 0;

        public DashboardWindow()
        {
            InitializeComponent();
            this.Loaded += DashboardWindow_Loaded;
        }

        public class ProjectDto
        {
            public int projectId { get; set; }
            public string projectName { get; set; }
        }

        // [MỚI] DTO cho Cột
        public class ColumnResponse
        {
            public int columnId { get; set; }
            public string columnName { get; set; }
            public int orderIndex { get; set; }
            public int projectId { get; set; }
        }

        private async void DashboardWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProjectsAsync();
        }

        // [MỚI] Hàm gọi API lấy danh sách tất cả Project thả vào ComboBox
        private async Task LoadProjectsAsync()
        {
            try
            {
                var projects = await client.GetFromJsonAsync<List<ProjectDto>>($"{AppConfig.ApiBaseUrl}/Projects");
                if (projects != null && projects.Count > 0)
                {
                    cboProjects.ItemsSource = projects;
                    cboProjects.SelectedIndex = 0; // Tự động chọn dự án đầu tiên trong danh sách
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách dự án: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // [MỚI] Khi người dùng đổi qua đổi lại giữa các Dự án
        // [ĐÃ SỬA] Thêm từ khóa 'async' để có thể gọi hàm API (await) bên trong
        private async void CboProjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 1. Sử dụng biến SelectedProjectId đã được khai báo ở đầu file để lấy ID dự án
            int projectId = SelectedProjectId;

            // 2. Chỉ tiếp tục nếu ID hợp lệ (lớn hơn 0)
            if (projectId > 0)
            {
                // 3. Gọi hàm tải toàn bộ dữ liệu (Cột và Task) từ API lên màn hình.
                // Hàm này đã tự động bao gồm chức năng dọn dẹp (ClearKanbanColumns) ở bên trong.
                await LoadProjectBoardAsync(projectId);
            }
        }

        private async Task LoadProjectBoardAsync(int projectId)
        {
            ClearKanbanColumns();

            try
            {
                // 1. Lấy danh sách cột
                var columns = await client.GetFromJsonAsync<List<ColumnResponse>>($"{AppConfig.ApiBaseUrl}/Columns/project/{projectId}");
                if (columns == null) return;

                // 2. Lấy danh sách task
                var tasks = await client.GetFromJsonAsync<List<KanbanColumn.TaskResponse>>($"{AppConfig.ApiBaseUrl}/Tasks/project/{projectId}");
                var taskList = tasks ?? new List<KanbanColumn.TaskResponse>();

                // 3. Vẽ cột và nạp task
                foreach (var col in columns.OrderBy(c => c.orderIndex))
                {
                    // Sửa constructor để truyền thêm ColumnId
                    var columnUi = new KanbanColumn(col.columnName, projectId, col.columnId);
                    spBoard.Children.Insert(spBoard.Children.Count - 1, columnUi);

                    var colTasks = taskList.Where(t => t.columnId == col.columnId);
                    foreach (var task in colTasks)
                        columnUi.AddTaskCard(task.taskId, task.taskName);
                }

                UpdateAddListButtonText();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không tải được dữ liệu bảng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearKanbanColumns()
        {
            for (int i = spBoard.Children.Count - 2; i >= 0; i--)
            {
                if (spBoard.Children[i] is KanbanColumn)
                    spBoard.Children.RemoveAt(i);
            }
            UpdateAddListButtonText();
        }

        private void btnMenuDashboard_Click(object sender, RoutedEventArgs e)
        {
            viewDashboard.Visibility = Visibility.Visible;
            viewCreate.Visibility = Visibility.Collapsed;
            btnMenuDashboard.Style = (Style)FindResource("ActiveMenuBtn");
            btnMenuCreate.Style = (Style)FindResource("MenuBtn");
        }

        private void btnMenuCreate_Click(object sender, RoutedEventArgs e)
        {
            viewCreate.Visibility = Visibility.Visible;
            viewDashboard.Visibility = Visibility.Collapsed;
            btnMenuCreate.Style = (Style)FindResource("ActiveMenuBtn");
            btnMenuDashboard.Style = (Style)FindResource("MenuBtn");
        }

        private async void BtnCreateUser_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUserName.Text)) return;
            var user = new { username = txtUserName.Text, email = txtEmail.Text, passwordHash = txtPassword.Password };
            var response = await client.PostAsJsonAsync($"{AppConfig.ApiBaseUrl}/Users/register", user);
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show("Tạo User thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                txtUserName.Clear();
                txtEmail.Clear();
                txtPassword.Clear();
            }
            else MessageBox.Show("Lỗi: " + await response.Content.ReadAsStringAsync(), "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async void BtnCreateProject_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtOwnerId.Text, out int ownerId)) return;
            var project = new { projectName = txtProjectName.Text, ownerId = ownerId };
            var response = await client.PostAsJsonAsync($"{AppConfig.ApiBaseUrl}/Projects", project);
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show("Tạo Dự án thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                txtProjectName.Clear();
                txtOwnerId.Clear();
                _ = LoadProjectsAsync(); // Reload combobox
            }
            else MessageBox.Show("Lỗi: Kiểm tra lại ID User.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async void BtnCreateTask_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtProjectId.Text, out int projectId)) return;
            var task = new { taskName = txtTaskName.Text, projectId = projectId };
            var response = await client.PostAsJsonAsync($"{AppConfig.ApiBaseUrl}/Tasks", task);
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show("Tạo Task thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                txtTaskName.Clear();
                txtProjectId.Clear();
                if (SelectedProjectId == projectId) _ = LoadProjectBoardAsync(projectId); // Reload board
            }
            else MessageBox.Show("Lỗi: Kiểm tra lại ID Dự án.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // 1. Ẩn nút, hiện ô nhập Title
        private void BtnShowAddList_Click(object sender, MouseButtonEventArgs e)
        {
            if (SelectedProjectId <= 0)
            {
                MessageBox.Show("Vui lòng chọn dự án trước khi thêm danh sách.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnShowAddList.Visibility = Visibility.Collapsed;
            panelAddListInput.Visibility = Visibility.Visible;

            Dispatcher.BeginInvoke(new System.Action(() => txtNewListName.Focus()));
        }

        // 2. Hủy tạo danh sách
        private void BtnCancelAddList_Click(object sender, RoutedEventArgs e)
        {
            txtNewListName.Text = "";
            panelAddListInput.Visibility = Visibility.Collapsed;
            btnShowAddList.Visibility = Visibility.Visible;
        }

        // 3. BỘ NÃO ĐÚC CỘT KANBAN:
        private async void AddNewList()
        {
            string title = txtNewListName.Text.Trim();
            if (string.IsNullOrEmpty(title)) return;

            // [MỚI] Lấy ID của dự án đang được hiển thị trên ComboBox
            if (cboProjects.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn một dự án trước khi tạo danh sách!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int selectedProjectId = (int)cboProjects.SelectedValue;

            try
            {
                // [MỚI] Gọi API để lưu cột xuống Database trước
                var newCol = new { columnName = title, projectId = selectedProjectId };
                var response = await client.PostAsJsonAsync($"{AppConfig.ApiBaseUrl}/Columns", newCol);
                
                if (response.IsSuccessStatusCode)
                {
                    var createdCol = await response.Content.ReadFromJsonAsync<ColumnResponse>();
                    if (createdCol != null)
                    {
                        // Đúc Cột mới và truyền thêm ID thực của Cột từ Database
                        KanbanColumn newColumn = new KanbanColumn(createdCol.columnName, selectedProjectId, createdCol.columnId);

                        spBoard.Children.Insert(spBoard.Children.Count - 1, newColumn);

                        txtNewListName.Text = "";
                        panelAddListInput.Visibility = Visibility.Collapsed;
                        btnShowAddList.Visibility = Visibility.Visible;

                        btnShowAddList.Focus();
                        UpdateAddListButtonText();
                    }
                }
                else
                {
                    MessageBox.Show("Lỗi khi tạo danh sách trên Server!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void UpdateAddListButtonText()
        {
            if (spBoard.Children.Count > 1)
                lblAddListText.Text = "Thêm danh sách khác";
            else
                lblAddListText.Text = "Thêm danh sách";
        }

        private void BtnConfirmAddList_Click(object sender, RoutedEventArgs e)
        {
            AddNewList();
        }

        private void TxtNewListName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                BtnCancelAddList_Click(null!, null!);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                AddNewList();
                e.Handled = true;
            }
        }

        private class ProjectItem
        {
            public int ProjectId { get; set; }
            public string ProjectName { get; set; } = string.Empty;
        }

    }
}
