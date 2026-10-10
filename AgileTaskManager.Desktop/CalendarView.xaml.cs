using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AgileTaskManager.Desktop.ViewModels;
using GongSolutions.Wpf.DragDrop;
using AgileTaskManager.Desktop.Services;
using System.Threading.Tasks;

namespace AgileTaskManager.Desktop
{
    public partial class CalendarView : UserControl, IDropTarget
    {
        private CalendarViewModel _viewModel;
        private KanbanBoardViewModel _boardVm;
        private ApiService _apiService;

        public CalendarView()
        {
            InitializeComponent();
            _viewModel = new CalendarViewModel();
            this.DataContext = _viewModel;
            _apiService = new ApiService();
            _viewModel.CurrentWeekStart = GetStartOfWeek(DateTime.Now);
            BuildGrid();
        }

        private DateTime GetStartOfWeek(DateTime dt)
        {
            int diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        public void RenderCalendar(KanbanBoardViewModel boardVm)
        {
            _boardVm = boardVm;
            UpdateCalendarData();
        }

        private void BuildGrid()
        {
            CalendarGrid.Children.Clear();
            CalendarGrid.RowDefinitions.Clear();
            CalendarGrid.ColumnDefinitions.Clear();

            // Cột 0: Thời gian. Cột 1-7: Thứ 2 -> Chủ nhật
            for (int i = 0; i < 8; i++)
            {
                CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = i == 0 ? new GridLength(80) : new GridLength(1, GridUnitType.Star) });
            }

            // Header hàng 0
            CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            AddCellToGrid("Slot / Day", 0, 0, true);
            for (int i = 0; i < 7; i++)
            {
                var dayDate = _viewModel.CurrentWeekStart.AddDays(i);
                string dayName = i == 6 ? "Chủ nhật" : $"Thứ {i + 2}";
                AddCellToGrid($"{dayName}\n{dayDate:dd/MM}", 0, i + 1, true);
            }

            // Dữ liệu thời gian: 8:00 đến 17:00 (10 slots)
            for (int row = 1; row <= 10; row++)
            {
                CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(100) });
                int hour = row + 7; // 8h, 9h,...
                AddCellToGrid($"{hour}:00", row, 0, true);

                for (int col = 1; col <= 7; col++)
                {
                    var cellDate = _viewModel.CurrentWeekStart.AddDays(col - 1).AddHours(hour);
                    
                    var cellVm = _viewModel.Cells.FirstOrDefault(c => c.SlotTime == cellDate);
                    if (cellVm == null)
                    {
                        cellVm = new CalendarCellViewModel { SlotTime = cellDate };
                        _viewModel.Cells.Add(cellVm);
                    }

                    var itemsControl = new ItemsControl
                    {
                        ItemsSource = cellVm.Tasks,
                        Margin = new Thickness(2),
                        AllowDrop = true,
                        ItemTemplate = (DataTemplate)FindResource("CalendarTaskTemplate"),
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        MinHeight = 80,
                        Background = Brushes.Transparent
                    };

                    // Gong DragDrop
                    GongSolutions.Wpf.DragDrop.DragDrop.SetIsDropTarget(itemsControl, true);
                    GongSolutions.Wpf.DragDrop.DragDrop.SetDropHandler(itemsControl, this);
                    
                    var border = new Border
                    {
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                        BorderThickness = new Thickness(0, 0, 1, 1),
                        Background = Brushes.Transparent,
                        Child = new ScrollViewer { Content = itemsControl, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }
                    };

                    Grid.SetRow(border, row);
                    Grid.SetColumn(border, col);
                    CalendarGrid.Children.Add(border);
                }
            }
            lblWeekInfo.Text = $"Tuần ({_viewModel.CurrentWeekStart:dd/MM} - {_viewModel.CurrentWeekStart.AddDays(6):dd/MM})";
        }

        

        private void AddCellToGrid(string text, int row, int col, bool isHeader)
        {
            var border = new Border
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Background = isHeader ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")) : Brushes.White,
                Padding = new Thickness(10)
            };
            
            var textBlock = new TextBlock
            {
                Text = text,
                FontWeight = isHeader ? FontWeights.Bold : FontWeights.Normal,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            border.Child = textBlock;
            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            CalendarGrid.Children.Add(border);
        }

        public void UpdateCalendarData()
        {
            if (_boardVm == null) return;

            _viewModel.Columns = _boardVm.Columns; // Ensure the ComboBox has columns to bind to

            foreach (var cell in _viewModel.Cells)
            {
                cell.Tasks.Clear();
            }
            _viewModel.UnscheduledTasks.Clear();

            var allTasks = _boardVm.Columns.SelectMany(c => c.Tasks);
            foreach (var task in allTasks)
            {
                if (task.DueDate.HasValue)
                {
                    var cell = _viewModel.Cells.FirstOrDefault(c => c.SlotTime.Date == task.DueDate.Value.Date && c.SlotTime.Hour == task.DueDate.Value.Hour);
                    if (cell != null)
                    {
                        cell.Tasks.Add(task);
                    }
                }
                else
                {
                    _viewModel.UnscheduledTasks.Add(task);
                }
            }
        }

        private void PrevWeek_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CurrentWeekStart = _viewModel.CurrentWeekStart.AddDays(-7);
            dtpSelectedDate.SelectedDate = _viewModel.CurrentWeekStart;
            BuildGrid();
            UpdateCalendarData();
        }

        private void CurrentWeek_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CurrentWeekStart = GetStartOfWeek(DateTime.Now);
            dtpSelectedDate.SelectedDate = _viewModel.CurrentWeekStart;
            BuildGrid();
            UpdateCalendarData();
        }

        private void DtpSelectedDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dtpSelectedDate.SelectedDate.HasValue)
            {
                _viewModel.CurrentWeekStart = GetStartOfWeek(dtpSelectedDate.SelectedDate.Value);
                BuildGrid();
                UpdateCalendarData();
            }
        }

        private void NextWeek_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CurrentWeekStart = _viewModel.CurrentWeekStart.AddDays(7);
            dtpSelectedDate.SelectedDate = _viewModel.CurrentWeekStart;
            BuildGrid();
            UpdateCalendarData();
        }

        private async void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var cb = sender as ComboBox;
                if (cb?.DataContext is KanbanTaskViewModel taskVm)
                {
                    if (cb.SelectedValue is int newColumnId)
                    {
                        if (taskVm.ColumnId != newColumnId)
                        {
                            taskVm.ColumnId = newColumnId;
                            await UpdateTaskApiAsync(taskVm);
                        }
                    }
                }
            }
        }

        // --- GONG DRAG DROP IMPLEMENTATION ---
        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.Data is KanbanTaskViewModel)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.Effects = DragDropEffects.Move;
            }
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            var task = dropInfo.Data as KanbanTaskViewModel;
            if (task == null) return;

            var targetItemsControl = dropInfo.VisualTarget as ItemsControl;
            if (targetItemsControl != null && targetItemsControl.ItemsSource is System.Collections.ObjectModel.ObservableCollection<KanbanTaskViewModel> targetList)
            {
                var sourceList = dropInfo.DragInfo.SourceCollection as System.Collections.ObjectModel.ObservableCollection<KanbanTaskViewModel>;
                if (sourceList != null && sourceList != targetList)
                {
                    // Find the corresponding cell if any
                    var cell = _viewModel.Cells.FirstOrDefault(c => c.Tasks == targetList);
                    
                    sourceList.Remove(task);
                    targetList.Insert(dropInfo.InsertIndex, task);
                    
                    if (cell != null)
                    {
                        // Thả vào lưới Calendar -> gán DueDate
                        task.DueDate = cell.SlotTime;
                    }
                    else if (targetList == _viewModel.UnscheduledTasks)
                    {
                        // Thả vào Unscheduled -> xóa DueDate
                        task.DueDate = null;
                    }
                    
                    _ = UpdateTaskApiAsync(task);
                }
            }
        }

        private async Task UpdateTaskApiAsync(KanbanTaskViewModel updatedVm)
        {
            int projectId = _boardVm.Columns.FirstOrDefault()?.ProjectId ?? 0;
            var updatePayload = new
            {
                TaskId = updatedVm.TaskId,
                TaskName = updatedVm.TaskName,
                Description = updatedVm.Description,
                AssigneeId = updatedVm.AssigneeId,
                DueDate = updatedVm.DueDate,
                Tags = updatedVm.Tags,
                ColumnId = updatedVm.ColumnId,
                OrderIndex = updatedVm.OrderIndex,
                ProjectId = projectId
            };
            await _apiService.UpdateTaskAsync(updatedVm.TaskId, updatePayload);
        }
    }
}






