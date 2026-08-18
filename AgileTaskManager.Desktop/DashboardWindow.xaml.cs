using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Input;

namespace AgileTaskManager.Desktop
{
    public partial class DashboardWindow : Window
    {
        private readonly string ApiBaseUrl = "http://localhost:5279/api";
        private static readonly HttpClient client = new HttpClient();
        private bool _isLoadingProjects;

        public int SelectedProjectId =>
            cboProject.SelectedValue is int id ? id : 0;

        public DashboardWindow()
        {
            InitializeComponent();
            Loaded += DashboardWindow_Loaded;
        }

        private async void DashboardWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProjectsAsync();
        }

        private async Task LoadProjectsAsync()
        {
            try
            {
                var projects = await client.GetFromJsonAsync<List<ProjectItem>>($"{ApiBaseUrl}/Projects");
                if (projects == null || projects.Count == 0)
                {
                    MessageBox.Show("Chưa có dự án nào. Hãy tạo dự án trước.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _isLoadingProjects = true;
                cboProject.ItemsSource = projects;
                cboProject.SelectedIndex = 0;
                _isLoadingProjects = false;

                await LoadProjectBoardAsync(SelectedProjectId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không tải được danh sách dự án: {ex.Message}\nBạn đã chạy backend chưa?", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CboProject_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isLoadingProjects || SelectedProjectId <= 0) return;
            await LoadProjectBoardAsync(SelectedProjectId);
        }

        private async Task LoadProjectBoardAsync(int projectId)
        {
            ClearKanbanColumns();

            try
            {
                var tasks = await client.GetFromJsonAsync<List<KanbanColumn.TaskResponse>>($"{ApiBaseUrl}/Tasks/project/{projectId}");
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
            if (SelectedProjectId <= 0)
            {
                MessageBox.Show("Vui lòng chọn dự án trước khi thêm danh sách.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string title = txtNewListName.Text.Trim();
            if (string.IsNullOrEmpty(title)) return;

            KanbanColumn newColumn = new KanbanColumn(title, SelectedProjectId);
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
