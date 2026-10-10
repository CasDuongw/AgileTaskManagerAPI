using AgileTaskManager.Desktop.ViewModels;
using System.Windows;
using System.Collections.Generic;

namespace AgileTaskManager.Desktop
{
    public partial class TaskDetailWindow : Window
    {
        public KanbanTaskViewModel TaskData { get; private set; }

        public TaskDetailWindow(KanbanTaskViewModel task, object users)
        {
            InitializeComponent();
            TaskData = new KanbanTaskViewModel
            {
                TaskId = task.TaskId,
                TaskName = task.TaskName,
                Description = task.Description,
                AssigneeId = task.AssigneeId,
                DueDate = task.DueDate,
                Tags = task.Tags,
                ColumnId = task.ColumnId,
                OrderIndex = task.OrderIndex
            };

            cboAssignee.ItemsSource = users as System.Collections.IEnumerable;

            // Load values into UI
            txtTaskName.Text = TaskData.TaskName;
            txtDescription.Text = TaskData.Description;
            dpDueDate.SelectedDate = TaskData.DueDate;
            txtTags.Text = TaskData.Tags;
            if (TaskData.AssigneeId.HasValue)
            {
                cboAssignee.SelectedValue = TaskData.AssigneeId.Value;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            TaskData.TaskName = txtTaskName.Text;
            TaskData.Description = txtDescription.Text;
            TaskData.DueDate = dpDueDate.SelectedDate;
            TaskData.Tags = txtTags.Text;
            
            if (cboAssignee.SelectedValue != null)
            {
                TaskData.AssigneeId = (int)cboAssignee.SelectedValue;
            }
            else
            {
                TaskData.AssigneeId = null;
            }

            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
