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
                var tasks = await client.GetFromJsonAsync<List<KanbanColumn.TaskResponse>>($"{AppConfig.ApiBaseUrl}/Tasks/project/{projectId}");
                if (tasks == null) return;

                foreach (var group in tasks.GroupBy(t => t.status))
                {
                    var column = new KanbanColumn(group.Key, projectId);
                    spBoard.Children.Insert(spBoard.Children.Count - 1, column);

                    foreach (var task in group)
                        column.AddTaskCard(task.taskId, task.taskName);
                }

                UpdateAddListButtonText();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không tải được task của dự án: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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

        // Mở cửa sổ tạo mới (Nút Menu bên trái)
        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            CreateWindow createWindow = new CreateWindow();
            createWindow.Show();
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
        private void AddNewList()
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

            // [SỬA] Đúc Cột mới và bơm đúng cái ID đang chọn vào trong Cột đó
            KanbanColumn newColumn = new KanbanColumn(title, selectedProjectId);

            spBoard.Children.Insert(spBoard.Children.Count - 1, newColumn);

            txtNewListName.Text = "";
            panelAddListInput.Visibility = Visibility.Collapsed;
            btnShowAddList.Visibility = Visibility.Visible;

            btnShowAddList.Focus();
            UpdateAddListButtonText();
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
