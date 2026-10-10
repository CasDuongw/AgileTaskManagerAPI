using AgileTaskManager.Desktop.ViewModels;
using System.Windows;
using System.Windows.Controls;
using AgileTaskManager.Desktop.Services;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace AgileTaskManager.Desktop
{
    public partial class TaskDetailWindow : Window
    {
        public KanbanTaskViewModel TaskData { get; private set; }
        private readonly ApiService _apiService;

        public TaskDetailWindow(KanbanTaskViewModel task, object users, ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
            TaskData = task;

            cboAssignee.ItemsSource = users as System.Collections.IEnumerable;

            // Load values into UI
            txtTaskName.Text = TaskData.TaskName;
            txtDescription.Text = TaskData.Description;
            dpDueDate.SelectedDate = TaskData.DueDate;
            cboAssignee.SelectedValue = TaskData.AssigneeId;
            txtTags.Text = TaskData.Tags;
            
            // Binding for subtasks
            icSubtasks.DataContext = TaskData;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            TaskData.TaskName = txtTaskName.Text;
            TaskData.Description = txtDescription.Text;
            TaskData.DueDate = dpDueDate.SelectedDate;
            TaskData.AssigneeId = cboAssignee.SelectedValue as int?;
            TaskData.Tags = txtTags.Text;

            this.DialogResult = true;
            this.Close();
        }

        private async void BtnAddSubtask_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNewSubtask.Text)) return;
            var subtaskTitle = txtNewSubtask.Text;
            txtNewSubtask.Clear();

            var newSubtask = new { title = subtaskTitle, isCompleted = false, taskId = TaskData.TaskId };
            var response = await _apiService.CreateSubtaskAsync(TaskData.TaskId, newSubtask);
            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<KanbanColumn.SubtaskResponse>();
                if (created != null)
                {
                    TaskData.Subtasks.Add(new SubtaskViewModel { SubtaskId = created.subtaskId, Title = created.title, IsCompleted = created.isCompleted });
                }
            }
        }

        private async void Subtask_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.Tag is SubtaskViewModel st)
            {
                await _apiService.UpdateSubtaskAsync(st.SubtaskId, new { title = st.Title, isCompleted = st.IsCompleted, subtaskId = st.SubtaskId, taskId = TaskData.TaskId });
            }
        }

        private async void Subtask_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Tag is SubtaskViewModel st)
            {
                await _apiService.UpdateSubtaskAsync(st.SubtaskId, new { title = st.Title, isCompleted = st.IsCompleted, subtaskId = st.SubtaskId, taskId = TaskData.TaskId });
            }
        }

        private async void BtnDeleteSubtask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is SubtaskViewModel st)
            {
                var response = await _apiService.DeleteSubtaskAsync(st.SubtaskId);
                if (response.IsSuccessStatusCode)
                {
                    TaskData.Subtasks.Remove(st);
                }
            }
        }
    }
}
