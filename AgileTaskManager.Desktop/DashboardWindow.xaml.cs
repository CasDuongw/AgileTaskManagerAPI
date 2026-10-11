using AgileTaskManager.Desktop.ViewModels;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace AgileTaskManager.Desktop
{
    public partial class DashboardWindow : Window
    {
        // Lấy URL cấu hình từ AppConfig
        // Đã xóa biến local ApiBaseUrl hardcode
        private readonly Services.ApiService _apiService;
        private KanbanBoardViewModel _viewModel;

        public int SelectedProjectId =>
            cboProjects.SelectedValue is int id ? id : 0;

        public DashboardWindow(Services.ApiService apiService, KanbanBoardViewModel viewModel)
        {
            InitializeComponent();
            _apiService = apiService;
            _viewModel = viewModel;
            this.DataContext = _viewModel;
            _viewModel.Columns.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (KanbanColumnViewModel col in e.NewItems)
                    {
                        col.Tasks.CollectionChanged += (ts, te) => 
                        {
                            if (viewCalendar?.Visibility == Visibility.Visible)
                                ucCalendarView?.UpdateCalendarData();
                        };
                    }
                }
                if (viewCalendar?.Visibility == Visibility.Visible && ucCalendarView != null)
                {
                    ucCalendarView.UpdateCalendarData();
                }
            };
            this.Loaded += DashboardWindow_Loaded;
        }

        public class ProjectDto
        {
            public int projectId { get; set; }
            public string projectName { get; set; }
        }

        public class UserDto
        {
            public int userId { get; set; }
            public string username { get; set; }
        }

        // [MỚI] DTO cho Cột
        public class ColumnResponse
        {
            public int columnId { get; set; }
            public string columnName { get; set; }
            public int orderIndex { get; set; }
            public int projectId { get; set; }
            public DateTime? dueDate { get; set; }
            public string status { get; set; }
        }

                private async void DashboardWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (AppConfig.CurrentUsername?.ToLower() != "admin")
            {
                panelCreateUser.Visibility = Visibility.Collapsed;
                System.Windows.Controls.Grid.SetColumn(panelCreateProject, 0);
                System.Windows.Controls.Grid.SetColumn(panelCreateTask, 1);
            }

            var timeSlots = new List<string>();
            for (int i = 0; i < 24; i++)
            {
                timeSlots.Add($"{i:D2}:00");
                timeSlots.Add($"{i:D2}:30");
            }
            cboCreateTaskTime.ItemsSource = timeSlots;

            await Task.WhenAll(LoadProjectsAsync(), LoadUsersAsync());
        }

        private async Task LoadProjectsAsync()
        {
            try
            {
                var projects = await _apiService.GetProjectsAsync<ProjectDto>();
                if (projects != null && projects.Count > 0)
                {
                    cboProjects.ItemsSource = projects;
                    cboProjects.SelectedIndex = 0; // Tự động chọn dự án đầu tiên trong danh sách

                    cboProject.ItemsSource = projects; // Cho form Create Task
                    if (cboProject.Items.Count > 0) cboProject.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách dự án: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var users = await _apiService.GetUsersAsync<UserDto>();
                if (users != null && users.Count > 0)
                {
                    if (AppConfig.CurrentUsername?.ToLower() != "admin")
                    {
                        users = users.Where(u => u.username.ToLower() == AppConfig.CurrentUsername?.ToLower()).ToList();
                    }
                    cboOwner.ItemsSource = users;
                    if (cboOwner.Items.Count > 0) cboOwner.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách user: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
            _viewModel.Columns.Clear();

            try
            {
                // 1 & 2. Gọi song song lấy Cột và lấy Task để rút ngắn 50% thời gian chờ mạng
                var columnsTask = _apiService.GetColumnsAsync<ColumnResponse>(projectId);
                var tasksTask = _apiService.GetTasksAsync<KanbanColumn.TaskResponse>(projectId);
                await Task.WhenAll(columnsTask, tasksTask);

                var columns = await columnsTask;
                if (columns == null) return;

                var tasks = await tasksTask;
                var taskList = tasks ?? new List<KanbanColumn.TaskResponse>();

                // 3. Nạp vào ViewModel
                foreach (var col in columns.OrderBy(c => c.orderIndex))
                {
                    var colVm = new KanbanColumnViewModel
                    {
                        ColumnId = col.columnId,
                        ColumnName = col.columnName,
                        ProjectId = projectId,
                        OrderIndex = col.orderIndex,
                        DueDate = col.dueDate.HasValue ? (col.dueDate.Value.Kind == DateTimeKind.Utc ? col.dueDate.Value.ToLocalTime() : col.dueDate.Value) : (DateTime?)null,
                        Status = string.IsNullOrEmpty(col.status) ? "Vừa giao" : col.status
                    };

                    var colTasks = taskList.Where(t => t.columnId == col.columnId).OrderBy(t => t.orderIndex);
                    foreach (var task in colTasks)
                    {
                        var taskVm = new KanbanTaskViewModel { TaskId = task.taskId, TaskName = task.taskName, Description = task.description, AssigneeId = task.assigneeId, DueDate = task.dueDate, Tags = task.tags, ColumnId = col.columnId, OrderIndex = task.orderIndex };
                        if (task.subtasks != null)
                        {
                            foreach (var st in task.subtasks)
                            {
                                taskVm.Subtasks.Add(new SubtaskViewModel { SubtaskId = st.subtaskId, Title = st.title, IsCompleted = st.isCompleted });
                            }
                        }
                        taskVm.EditTaskCommand = new RelayCommand(async () => await EditTaskAsync(taskVm));
                        colVm.Tasks.Add(taskVm);
                    }
                      colVm.Tasks.CollectionChanged += (s, ev) => { if (ucCalendarView != null) ucCalendarView.UpdateCalendarData(); };
                    _viewModel.Columns.Add(colVm);
                }

                UpdateAddListButtonText();
                if (ucCalendarView != null) ucCalendarView.RenderCalendar(_viewModel);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không tải được dữ liệu bảng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearKanbanColumns()
        {
            _viewModel.Columns.Clear();
            UpdateAddListButtonText();
            if (ucCalendarView != null) ucCalendarView.UpdateCalendarData();
        }

        private void btnMenuDashboard_Click(object sender, RoutedEventArgs e)
        {
            viewDashboard.Visibility = Visibility.Visible;
            viewCreate.Visibility = Visibility.Collapsed;
            if (viewCalendar != null) viewCalendar.Visibility = Visibility.Collapsed;
            btnMenuDashboard.Style = (Style)FindResource("ActiveMenuBtn");
            btnMenuCalendar.Style = (Style)FindResource("MenuBtn");
            btnMenuCreate.Style = (Style)FindResource("MenuBtn");
        }

        private void btnMenuCreate_Click(object sender, RoutedEventArgs e)
        {
            viewCreate.Visibility = Visibility.Visible;
            viewDashboard.Visibility = Visibility.Collapsed;
            if (viewCalendar != null) viewCalendar.Visibility = Visibility.Collapsed;
            btnMenuCreate.Style = (Style)FindResource("ActiveMenuBtn");
            btnMenuCalendar.Style = (Style)FindResource("MenuBtn");
            btnMenuDashboard.Style = (Style)FindResource("MenuBtn");
        }

        private async void BtnCreateUser_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUserName.Text)) return;
            var user = new { username = txtUserName.Text, email = txtEmail.Text, passwordHash = txtPassword.Password };
            var response = await _apiService.RegisterUserAsync(user);
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show("Tạo User thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                txtUserName.Clear();
                txtEmail.Clear();
                txtPassword.Clear();
                _ = LoadUsersAsync(); // Reload combobox
            }
            else MessageBox.Show("Lỗi: " + await response.Content.ReadAsStringAsync(), "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async void BtnCreateProject_Click(object sender, RoutedEventArgs e)
        {
            if (cboOwner.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn Owner.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int ownerId = (int)cboOwner.SelectedValue;
            var project = new { projectName = txtProjectName.Text, ownerId = ownerId };
            var response = await _apiService.CreateProjectAsync(project);
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show("Tạo Dự án thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                txtProjectName.Clear();
                _ = LoadProjectsAsync(); // Reload combobox
            }
            else MessageBox.Show("Lỗi: Không thể tạo dự án.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private async void BtnCreateTask_Click(object sender, RoutedEventArgs e)
        {
            if (cboProject.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn Dự án.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int projectId = (int)cboProject.SelectedValue;
            var task = new { taskName = txtTaskName.Text, projectId = projectId };
            var response = await _apiService.CreateTaskAsync(task);
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show("Tạo Task thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                txtTaskName.Clear();
                
                // Nếu Task mới thuộc Project đang mở, thêm trực tiếp vào ViewModel thay vì reload toàn bộ bảng
                if (SelectedProjectId == projectId) 
                {
                    var createdTask = await response.Content.ReadFromJsonAsync<KanbanColumn.TaskResponse>();
                    if (createdTask != null)
                    {
                        var column = _viewModel.Columns.FirstOrDefault(c => c.ColumnId == createdTask.columnId);
                        if (column != null)
                        {
                            var taskVm = new KanbanTaskViewModel { TaskId = createdTask.taskId, TaskName = createdTask.taskName, Description = createdTask.description, AssigneeId = createdTask.assigneeId, DueDate = createdTask.dueDate, Tags = createdTask.tags, ColumnId = createdTask.columnId, OrderIndex = createdTask.orderIndex };
                              taskVm.EditTaskCommand = new RelayCommand(async () => await EditTaskAsync(taskVm));
                              column.Tasks.Add(taskVm);
                              if (ucCalendarView != null) ucCalendarView.UpdateCalendarData();
                        }
                    }
                }
            }
            else MessageBox.Show("Lỗi: Không thể tạo Task.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                var response = await _apiService.CreateColumnAsync(newCol);
                
                if (response.IsSuccessStatusCode)
                {
                    var createdCol = await response.Content.ReadFromJsonAsync<ColumnResponse>();
                    if (createdCol != null)
                    {
                        var colVm = new KanbanColumnViewModel
                        {
                            ColumnId = createdCol.columnId,
                            ColumnName = createdCol.columnName,
                            ProjectId = selectedProjectId
                        };
                        colVm.Tasks.CollectionChanged += (s, ev) => { if (ucCalendarView != null) ucCalendarView.UpdateCalendarData(); };
                        _viewModel.Columns.Add(colVm);

                        txtNewListName.Text = "";
                        panelAddListInput.Visibility = Visibility.Collapsed;
                        btnShowAddList.Visibility = Visibility.Visible;

                        btnShowAddList.Focus();
                        UpdateAddListButtonText();
                        if (ucCalendarView != null) ucCalendarView.UpdateCalendarData();
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
            if (_viewModel != null && _viewModel.Columns.Count > 0)
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

    
        private void btnMenuCalendar_Click(object sender, RoutedEventArgs e)
        {
            viewCalendar.Visibility = Visibility.Visible;
            viewDashboard.Visibility = Visibility.Collapsed;
            viewCreate.Visibility = Visibility.Collapsed;
            
            btnMenuCalendar.Style = (Style)FindResource("ActiveMenuBtn");
            btnMenuDashboard.Style = (Style)FindResource("MenuBtn");
            btnMenuCreate.Style = (Style)FindResource("MenuBtn");

            if (ucCalendarView != null)
            {
                ucCalendarView.RenderCalendar(_viewModel);
            }
        }
    
        private async Task EditTaskAsync(KanbanTaskViewModel taskVm)
        {
            var detailWindow = new TaskDetailWindow(taskVm, cboOwner.ItemsSource, _apiService);
            if (detailWindow.ShowDialog() == true)
            {
                var updatedVm = detailWindow.TaskData;
                if (updatedVm != null)
                {
                    taskVm.TaskName = updatedVm.TaskName;
                    taskVm.Description = updatedVm.Description;
                    taskVm.AssigneeId = updatedVm.AssigneeId;
                    taskVm.DueDate = updatedVm.DueDate;
                    taskVm.Tags = updatedVm.Tags;

                    DateTime? dueDateUtc = null;
                    if (taskVm.DueDate.HasValue)
                    {
                        var dt = taskVm.DueDate.Value;
                        DateTime localDt = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
                        localDt = DateTime.SpecifyKind(localDt, DateTimeKind.Local);
                        dueDateUtc = localDt.ToUniversalTime();
                    }

                    var updatePayload = new
                    {
                        TaskId = taskVm.TaskId,
                        TaskName = taskVm.TaskName,
                        Description = taskVm.Description,
                        AssigneeId = taskVm.AssigneeId,
                        DueDate = dueDateUtc,
                        Tags = taskVm.Tags,
                        ColumnId = taskVm.ColumnId,
                        ProjectId = SelectedProjectId,
                        OrderIndex = taskVm.OrderIndex,
                        IsActive = true
                    };

                    try
                    {
                        var response = await _apiService.UpdateTaskAsync(taskVm.TaskId, updatePayload);
                        if (!response.IsSuccessStatusCode)
                        {
                            var err = await response.Content.ReadAsStringAsync();
                            MessageBox.Show($"Lỗi lưu Task lên Server: {err}", "Lỗi Server", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi kết nối khi lưu Task: {ex.Message}", "Lỗi mạng", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    if (viewCalendar?.Visibility == Visibility.Visible && ucCalendarView != null) 
                        ucCalendarView.UpdateCalendarData();
                }
            }
        }
    }
}





