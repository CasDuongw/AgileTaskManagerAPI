using ControlzEx.Standard;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using System.Net.Http;
using System.Net.Http.Json;
using AgileTaskManager.Desktop.ViewModels;
using System.Linq;

namespace AgileTaskManager.Desktop
{
    public partial class KanbanColumn : UserControl
    {
        // Lấy URL cấu hình từ AppConfig
        // Đã xóa biến local ApiBaseUrl hardcode
        private static readonly HttpClient client = AppConfig.Client;

        public KanbanColumn()
        {
            InitializeComponent();
            
            // Chặn việc UserControl tự bắt tiêu điểm bàn phím/chuột gây ra viền nét đứt
            IsTabStop = false;
            Focusable = false;

            // Hover chuột vào tiêu đề -> Hiện nút Xóa cột
            titleGrid.MouseEnter += (s, e) => btnDeleteColumn.Visibility = Visibility.Visible;
            titleGrid.MouseLeave += (s, e) => btnDeleteColumn.Visibility = Visibility.Hidden;



        }
        public class TaskResponse
        {
            public int taskId { get; set; }
            public string taskName { get; set; }
            public int columnId { get; set; }
            public int projectId { get; set; }
        }


        private async void BtnDeleteColumn_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is KanbanColumnViewModel colVm)
            {
                try
                {
                    var response = await client.DeleteAsync($"{AppConfig.ApiBaseUrl}/Columns/{colVm.ColumnId}");
                    if (response.IsSuccessStatusCode)
                    {
                        Window window = Window.GetWindow(this);
                        if (window is DashboardWindow dashboard && dashboard.DataContext is KanbanBoardViewModel boardVm)
                        {
                            boardVm.Columns.Remove(colVm);
                            dashboard.UpdateAddListButtonText();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Loi xoa cot tren server!", "Loi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Loi ket noi: {ex.Message}", "Loi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ----- CÁC LỆNH XỬ LÝ THÊM/XÓA THẺ TỪ TRƯỚC ĐẾN NAY -----
        private void BtnShowAddCard_Click(object sender, RoutedEventArgs e)
        {
            btnShowAddCard.Visibility = Visibility.Collapsed;
            panelAddCardInput.Visibility = Visibility.Visible;
            
            // [GIẢI QUYẾT VẤN ĐỀ 2] Đợi UI vẽ xong mới ép Focus vào ô nhập liệu
            Dispatcher.BeginInvoke(new System.Action(() => txtNewTaskName.Focus()));
        }

        private void BtnCancelAddCard_Click(object sender, RoutedEventArgs e)
        {
            txtNewTaskName.Text = "";
            panelAddCardInput.Visibility = Visibility.Collapsed;
            btnShowAddCard.Visibility = Visibility.Visible;
        }

        private async void AddNewTask()
        {
            string taskContent = txtNewTaskName.Text.Trim();
            if (string.IsNullOrEmpty(taskContent)) return;

            var colVm = this.DataContext as KanbanColumnViewModel;
            if (colVm == null) return;

            var newTask = new
            {
                taskName = taskContent,
                projectId = colVm.ProjectId,
                columnId = colVm.ColumnId
            };

            try
            {
                var response = await client.PostAsJsonAsync($"{AppConfig.ApiBaseUrl}/Tasks", newTask);

                if (response.IsSuccessStatusCode)
                {
                    var createdTask = await response.Content.ReadFromJsonAsync<TaskResponse>();
                    
                    colVm.Tasks.Add(new KanbanTaskViewModel { TaskId = createdTask.taskId, TaskName = createdTask.taskName, ColumnId = createdTask.columnId });

                    txtNewTaskName.Text = "";
                    txtNewTaskName.Focus();
                }
                else
                {
                    string errorMsg = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi khi lưu Task vào Database!\nChi tiết: {errorMsg}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối API: {ex.Message}\nBạn đã chạy backend chưa?", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnDeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is KanbanTaskViewModel taskVm)
            {
                if (this.DataContext is KanbanColumnViewModel colVm)
                {
                    try
                    {
                        var response = await client.DeleteAsync($"{AppConfig.ApiBaseUrl}/Tasks/{taskVm.TaskId}");
                        if (response.IsSuccessStatusCode)
                        {
                            colVm.Tasks.Remove(taskVm);
                        }
                        else
                        {
                            MessageBox.Show("Loi xoa task tren server!", "Loi", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Loi ket noi: {ex.Message}", "Loi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }



        private void BtnConfirmAddCard_Click(object sender, RoutedEventArgs e)
        {
            AddNewTask();
        }
        
        private void TxtNewTaskName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // [GIẢI QUYẾT VẤN ĐỀ 3] Nếu bấm ESC -> Tự động gọi hàm Hủy (đóng form)
            if (e.Key == Key.Escape)
            {
                BtnCancelAddCard_Click(null, null);
                e.Handled = true;
                return;
            }

            // Nếu bấm Enter -> Lưu thẻ
            if (e.Key == Key.Enter)
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift) return; // Shift+Enter thì xuống dòng

                AddNewTask();
                e.Handled = true;

                // [GIẢI QUYẾT VẤN ĐỀ 1] Ép con trỏ chuột quay lại ô nhập liệu ngay lập tức để gõ liên tục
                Dispatcher.BeginInvoke(new System.Action(() => txtNewTaskName.Focus()));
            }
        }

        // ==============================================================================
        // TÍNH NĂNG DRAG & DROP (KÉO THẢ CỘT VÀ TASK) 
        // ==============================================================================

        private Point _dragStartPoint;

        private void TitleGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void TitleGrid_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            Point mousePos = e.GetPosition(null);
            Vector diff = _dragStartPoint - mousePos;
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                if (this.DataContext is KanbanColumnViewModel colVm)
                {
                    DragDrop.DoDragDrop(this, colVm, DragDropEffects.Move);
                }
            }
        }

        private void Task_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Button) return; // Không drag khi click nút xóa
            _dragStartPoint = e.GetPosition(null);
        }

        private void Task_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            Point mousePos = e.GetPosition(null);
            Vector diff = _dragStartPoint - mousePos;
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                if (sender is Border border && border.DataContext is KanbanTaskViewModel taskVm)
                {
                    DragDrop.DoDragDrop(border, taskVm, DragDropEffects.Move);
                }
            }
        }

        private void UserControl_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(KanbanColumnViewModel)) && e.Data.GetData(typeof(KanbanColumnViewModel)) != this.DataContext)
            {
                e.Effects = DragDropEffects.Move;
            }
            else if (e.Data.GetDataPresent(typeof(KanbanTaskViewModel)))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void UserControl_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(KanbanColumnViewModel)))
            {
                var droppedColumn = e.Data.GetData(typeof(KanbanColumnViewModel)) as KanbanColumnViewModel;
                var targetColumn = this.DataContext as KanbanColumnViewModel;

                if (droppedColumn != null && targetColumn != null && droppedColumn != targetColumn)
                {
                    Window window = Window.GetWindow(this);
                    if (window is DashboardWindow dashboard && dashboard.DataContext is KanbanBoardViewModel boardVm)
                    {
                        int targetIndex = boardVm.Columns.IndexOf(targetColumn);
                        boardVm.Columns.Remove(droppedColumn);
                        boardVm.Columns.Insert(targetIndex, droppedColumn);
                        
                        SyncColumnOrderToServer(boardVm);
                    }
                }
            }
            else if (e.Data.GetDataPresent(typeof(KanbanTaskViewModel)))
            {
                var droppedTask = e.Data.GetData(typeof(KanbanTaskViewModel)) as KanbanTaskViewModel;
                var targetColumn = this.DataContext as KanbanColumnViewModel;

                if (droppedTask != null && targetColumn != null)
                {
                    Window window = Window.GetWindow(this);
                    if (window is DashboardWindow dashboard && dashboard.DataContext is KanbanBoardViewModel boardVm)
                    {
                        var sourceColumn = boardVm.Columns.FirstOrDefault(c => c.Tasks.Contains(droppedTask));
                        if (sourceColumn != null)
                        {
                            int insertIndex = targetColumn.Tasks.Count;
                            for (int i = 0; i < icTaskList.Items.Count; i++)
                            {
                                var container = icTaskList.ItemContainerGenerator.ContainerFromIndex(i) as UIElement;
                                if (container != null)
                                {
                                    Point p = e.GetPosition(container);
                                    if (p.Y < container.RenderSize.Height / 2)
                                    {
                                        insertIndex = i;
                                        break;
                                    }
                                }
                            }

                            int oldIndex = sourceColumn.Tasks.IndexOf(droppedTask);
                            int newIndex = insertIndex;

                            if (sourceColumn == targetColumn)
                            {
                                if (oldIndex < newIndex) newIndex--;
                                if (oldIndex == newIndex) return; // No change
                            }

                            sourceColumn.Tasks.Remove(droppedTask);
                            droppedTask.ColumnId = targetColumn.ColumnId;
                            
                            if (newIndex > targetColumn.Tasks.Count) newIndex = targetColumn.Tasks.Count;
                            if (newIndex < 0) newIndex = 0;

                            targetColumn.Tasks.Insert(newIndex, droppedTask);

                            if (sourceColumn != targetColumn)
                            {
                                SyncCardStatusAndOrderToServer(droppedTask, sourceColumn, targetColumn);
                            }
                            else
                            {
                                SyncTaskOrderToServer(targetColumn);
                            }
                        }
                    }
                }
            }
            e.Handled = true;
        }

        private async void SyncColumnOrderToServer(KanbanBoardViewModel boardVm)
        {
            var reorderList = boardVm.Columns.Select((col, index) => new { ColumnId = col.ColumnId, OrderIndex = index }).ToList();
            try
            {
                await client.PutAsJsonAsync($"{AppConfig.ApiBaseUrl}/Columns/reorder", reorderList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi đồng bộ thứ tự cột: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void SyncCardStatusAndOrderToServer(KanbanTaskViewModel task, KanbanColumnViewModel oldCol, KanbanColumnViewModel newCol)
        {
            try
            {
                var response = await client.PatchAsJsonAsync($"{AppConfig.ApiBaseUrl}/Tasks/{task.TaskId}/column", newCol.ColumnId);
                if (response.IsSuccessStatusCode)
                {
                    SyncTaskOrderToServer(newCol);
                }
                else
                {
                    MessageBox.Show("Server tu choi cap nhat!", "Loi", MessageBoxButton.OK, MessageBoxImage.Error);
                    newCol.Tasks.Remove(task);
                    task.ColumnId = oldCol.ColumnId;
                    oldCol.Tasks.Add(task);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Loi mang: {ex.Message}", "Loi", MessageBoxButton.OK, MessageBoxImage.Error);
                newCol.Tasks.Remove(task);
                task.ColumnId = oldCol.ColumnId;
                oldCol.Tasks.Add(task);
            }
        }

        private async void SyncTaskOrderToServer(KanbanColumnViewModel col)
        {
            var reorderList = col.Tasks.Select((t, index) => new { TaskId = t.TaskId, OrderIndex = index }).ToList();
            try
            {
                await client.PutAsJsonAsync($"{AppConfig.ApiBaseUrl}/Tasks/reorder", reorderList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Loi dong bo thu tu task: {ex.Message}", "Loi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            if (child == null) return null;
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindParent<T>(parentObject);
        }
    }
}
