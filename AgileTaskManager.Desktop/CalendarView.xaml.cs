using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AgileTaskManager.Desktop.ViewModels;
using GongSolutions.Wpf.DragDrop;
using AgileTaskManager.Desktop.Services;
using System.Threading.Tasks;
using System.Collections.Generic;

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

        private async void TxtColumnName_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb?.DataContext is KanbanColumnViewModel vm)
            {
                vm.IsEditingTitle = false;
                await UpdateColumnApiAsync(vm);
            }
        }

        private void TxtCalendarTitle_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is TextBox tb && tb.Visibility == Visibility.Visible)
            {
                tb.Focus();
                tb.SelectAll();
            }
        }

        private void TxtColumnName_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                System.Windows.Input.Keyboard.ClearFocus();
                var uiElement = e.OriginalSource as UIElement;
                if (uiElement != null)
                {
                    uiElement.MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next));
                }
            }
        }

        private DateTime GetStartOfWeek(DateTime dt)
        {
            int diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        private bool _isUpdatingDate = false;

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
            _viewModel.Cells.Clear();

            for (int i = 0; i < 8; i++)
            {
                CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition { MinWidth = i == 0 ? 80 : 120, Width = i == 0 ? new GridLength(80) : new GridLength(1, GridUnitType.Star) });
            }

            CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            AddCellToGrid("Slot / Day", 0, 0, true);
            for (int i = 0; i < 7; i++)
            {
                var dayDate = _viewModel.CurrentWeekStart.AddDays(i);
                string dayName = i == 6 ? "Chủ nhật" : $"Thứ {i + 2}";
                AddCellToGrid($"{dayName}\n{dayDate:dd/MM}", 0, i + 1, true);
            }

            for (int row = 1; row <= 10; row++)
            {
                CalendarGrid.RowDefinitions.Add(new RowDefinition { MinHeight = 100, Height = GridLength.Auto });
                int hour = row + 7;
                AddCellToGrid($"{hour}:00", row, 0, true);

                for (int col = 1; col <= 7; col++)
                {
                    var cellDate = _viewModel.CurrentWeekStart.AddDays(col - 1).AddHours(hour);
                    
                    var cellVm = new CalendarCellViewModel { SlotTime = cellDate };
                    _viewModel.Cells.Add(cellVm);

                    var itemsControl = new ItemsControl
                    {
                        ItemsSource = cellVm.Tasks,
                        Margin = new Thickness(2),
                        AllowDrop = true,
                        ItemTemplate = (DataTemplate)FindResource("CalendarTaskTemplate"),
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        MinHeight = 80,
                        Background = Brushes.Transparent,
                        Tag = cellVm
                    };

                    GongSolutions.Wpf.DragDrop.DragDrop.SetIsDropTarget(itemsControl, true);
                    GongSolutions.Wpf.DragDrop.DragDrop.SetDropHandler(itemsControl, this);
                    GongSolutions.Wpf.DragDrop.DragDrop.SetIsDragSource(itemsControl, true);
                    
                    var scrollViewer = new ScrollViewer 
                    { 
                        Content = itemsControl, 
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Tag = cellVm,
                        AllowDrop = true
                    };
                    GongSolutions.Wpf.DragDrop.DragDrop.SetIsDropTarget(scrollViewer, true);
                    GongSolutions.Wpf.DragDrop.DragDrop.SetDropHandler(scrollViewer, this);

                    var border = new Border
                    {
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                        BorderThickness = new Thickness(0, 0, 1, 1),
                        Background = Brushes.Transparent,
                        Child = scrollViewer,
                        Tag = cellVm,
                        AllowDrop = true
                    };
                    GongSolutions.Wpf.DragDrop.DragDrop.SetIsDropTarget(border, true);
                    GongSolutions.Wpf.DragDrop.DragDrop.SetDropHandler(border, this);

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
            if (_boardVm == null)
            {
                Window window = Window.GetWindow(this);
                if (window is DashboardWindow dashboard && dashboard.DataContext is KanbanBoardViewModel boardVm)
                {
                    _boardVm = boardVm;
                }
            }

            if (_boardVm == null) return;

            if (_viewModel.Cells.Count == 0)
            {
                BuildGrid();
            }

            _viewModel.Columns = _boardVm.Columns;

            foreach (var cell in _viewModel.Cells)
            {
                cell.Tasks.Clear();
            }
            _viewModel.UnscheduledTasks.Clear();

            foreach (var column in _boardVm.Columns)
            {
                if (column.DueDate.HasValue)
                {
                    DateTime due = column.DueDate.Value.Kind == DateTimeKind.Utc 
                        ? column.DueDate.Value.ToLocalTime() 
                        : column.DueDate.Value;

                    var cell = _viewModel.Cells.FirstOrDefault(c => c.SlotTime.Date == due.Date && c.SlotTime.Hour == due.Hour);
                    if (cell != null)
                    {
                        cell.Tasks.Add(column);
                    }
                    else
                    {
                        // Nếu giờ không nằm trong khung 8..17h (ví dụ 00:00 midnight) hoặc thuộc tuần hiện tại mà không khớp slot:
                        if (due.Hour < 8 || due.Hour > 17 || (due.Date >= _viewModel.CurrentWeekStart && due.Date < _viewModel.CurrentWeekStart.AddDays(7)))
                        {
                            _viewModel.UnscheduledTasks.Add(column);
                        }
                    }
                }
                else
                {
                    _viewModel.UnscheduledTasks.Add(column);
                }
            }
        }

        private void PrevWeek_Click(object sender, RoutedEventArgs e)
        {
            _isUpdatingDate = true;
            _viewModel.CurrentWeekStart = _viewModel.CurrentWeekStart.AddDays(-7);
            dtpSelectedDate.SelectedDate = _viewModel.CurrentWeekStart;
            _isUpdatingDate = false;
            BuildGrid();
            UpdateCalendarData();
        }

        private void CurrentWeek_Click(object sender, RoutedEventArgs e)
        {
            _isUpdatingDate = true;
            _viewModel.CurrentWeekStart = GetStartOfWeek(DateTime.Now);
            dtpSelectedDate.SelectedDate = _viewModel.CurrentWeekStart;
            _isUpdatingDate = false;
            BuildGrid();
            UpdateCalendarData();
        }

        private void DtpSelectedDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingDate) return;
            if (dtpSelectedDate.SelectedDate.HasValue)
            {
                _viewModel.CurrentWeekStart = GetStartOfWeek(dtpSelectedDate.SelectedDate.Value);
                BuildGrid();
                UpdateCalendarData();
            }
        }

        private void NextWeek_Click(object sender, RoutedEventArgs e)
        {
            _isUpdatingDate = true;
            _viewModel.CurrentWeekStart = _viewModel.CurrentWeekStart.AddDays(7);
            dtpSelectedDate.SelectedDate = _viewModel.CurrentWeekStart;
            _isUpdatingDate = false;
            BuildGrid();
            UpdateCalendarData();
        }

        private async void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.RemovedItems.Count > 0)
            {
                var cb = sender as ComboBox;
                if (cb?.DataContext is KanbanColumnViewModel columnVm)
                {
                    string? newStatus = e.AddedItems[0] as string;
                    if (!string.IsNullOrEmpty(newStatus))
                    {
                        columnVm.Status = newStatus;
                        await UpdateColumnApiAsync(columnVm);
                    }
                }
            }
        }

        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.Data is KanbanColumnViewModel)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.Effects = DragDropEffects.Move;
            }
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            var column = dropInfo.Data as KanbanColumnViewModel;
            if (column == null) return;

            if (dropInfo.VisualTarget == txtUnscheduledHeader)
            {
                var sourceList = dropInfo.DragInfo.SourceCollection as System.Collections.ObjectModel.ObservableCollection<KanbanColumnViewModel>;
                if (sourceList != null && sourceList != _viewModel.UnscheduledTasks)
                {
                    sourceList.Remove(column);
                    _viewModel.UnscheduledTasks.Insert(0, column);
                    column.DueDate = null;
                    _ = UpdateColumnApiAsync(column);
                }
                else if (sourceList == _viewModel.UnscheduledTasks)
                {
                    sourceList.Remove(column);
                    _viewModel.UnscheduledTasks.Insert(0, column);
                }
                return;
            }

            var targetList = dropInfo.TargetCollection as System.Collections.ObjectModel.ObservableCollection<KanbanColumnViewModel>
                ?? (dropInfo.VisualTarget as ItemsControl)?.ItemsSource as System.Collections.ObjectModel.ObservableCollection<KanbanColumnViewModel>;

            CalendarCellViewModel targetCell = null;

            if (dropInfo.VisualTarget is FrameworkElement fe)
            {
                if (fe.Tag is CalendarCellViewModel cellFromTag)
                {
                    targetCell = cellFromTag;
                    targetList = targetCell.Tasks;
                }
                else if (fe is ScrollViewer sv && sv.Tag is CalendarCellViewModel cellFromSv)
                {
                    targetCell = cellFromSv;
                    targetList = targetCell.Tasks;
                }
                else if (fe is Border b && b.Tag is CalendarCellViewModel cellFromB)
                {
                    targetCell = cellFromB;
                    targetList = targetCell.Tasks;
                }
                else
                {
                    var current = fe.Parent as FrameworkElement;
                    while (current != null)
                    {
                        if (current.Tag is CalendarCellViewModel cellFromParent)
                        {
                            targetCell = cellFromParent;
                            targetList = targetCell.Tasks;
                            break;
                        }
                        if (current is ItemsControl ic && ic.ItemsSource is System.Collections.ObjectModel.ObservableCollection<KanbanColumnViewModel> list)
                        {
                            targetList = list;
                            break;
                        }
                        current = current.Parent as FrameworkElement;
                    }
                }
            }

            if (targetList != null)
            {
                var sourceList = dropInfo.DragInfo.SourceCollection as System.Collections.ObjectModel.ObservableCollection<KanbanColumnViewModel>;
                if (sourceList != null && sourceList != targetList)
                {
                    var cell = targetCell ?? _viewModel.Cells.FirstOrDefault(c => c.Tasks == targetList);
                    
                    sourceList.Remove(column);
                    int insertIndex = Math.Min(Math.Max(0, dropInfo.InsertIndex), targetList.Count);
                    targetList.Insert(insertIndex, column);
                    
                    if (cell != null)
                    {
                        column.DueDate = cell.SlotTime;
                        _ = UpdateColumnApiAsync(column);
                    }
                    else if (targetList == _viewModel.UnscheduledTasks)
                    {
                        column.DueDate = null;
                        _ = UpdateColumnApiAsync(column);
                    }
                }
                else if (sourceList == targetList)
                {
                    GongSolutions.Wpf.DragDrop.DragDrop.DefaultDropHandler.Drop(dropInfo);
                }
            }
        }

        private async Task UpdateColumnApiAsync(KanbanColumnViewModel updatedVm)
        {
            DateTime? dueDateUtc = null;
            if (updatedVm.DueDate.HasValue)
            {
                var dt = updatedVm.DueDate.Value;
                DateTime localDt = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
                localDt = DateTime.SpecifyKind(localDt, DateTimeKind.Local);
                dueDateUtc = localDt.ToUniversalTime();
            }

            var updatePayload = new
            {
                ColumnId = updatedVm.ColumnId,
                ColumnName = updatedVm.ColumnName,
                OrderIndex = updatedVm.OrderIndex,
                ProjectId = updatedVm.ProjectId,
                DueDate = dueDateUtc,
                Status = updatedVm.Status,
                IsActive = true
            };
            try
            {
                var response = await _apiService.UpdateColumnAsync(updatedVm.ColumnId, updatePayload);
                if (!response.IsSuccessStatusCode)
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi khi lưu lịch vào Database!\nChi tiết: {msg}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối khi cập nhật lịch: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}