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
using System.Windows.Input;

namespace AgileTaskManager.Desktop
{
    public partial class CalendarView : UserControl, IDropTarget
    {
        public enum CalendarViewMode { Day, Week }

        private static readonly SolidColorBrush PrimaryBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#425B9A"));
        private static readonly SolidColorBrush TextMutedBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));

        private DataTemplate? _taskTemplate;
        private DataTemplate TaskTemplate => _taskTemplate ??= (DataTemplate)Resources["CalendarTaskTemplate"];

        private CalendarViewModel _viewModel;
        private KanbanBoardViewModel? _boardVm;
        private ApiService _apiService;
        private CalendarViewMode _viewMode = CalendarViewMode.Week;
        private DateTime _selectedDate = DateTime.Today;
        private bool _isUpdatingDate = false;

        public CalendarView()
        {
            InitializeComponent();
            _viewModel = new CalendarViewModel();
            this.DataContext = _viewModel;
            _apiService = new ApiService();
            _selectedDate = DateTime.Today;
            _viewModel.CurrentWeekStart = GetStartOfWeek(_selectedDate);
            _isUpdatingDate = true;
            dtpSelectedDate.SelectedDate = _selectedDate;
            _isUpdatingDate = false;
            _viewMode = CalendarViewMode.Week;
            BuildGrid();
        }

        /// <summary>
        /// Đặt lại chế độ xem mặc định là Tuần và cập nhật dữ liệu lịch
        /// </summary>
        public void ResetToWeekView()
        {
            _viewMode = CalendarViewMode.Week;
            BuildGrid();
            UpdateCalendarData();
        }

        public void SetSideBySideMode(bool isSideBySide)
        {
            _viewMode = isSideBySide ? CalendarViewMode.Day : CalendarViewMode.Week;
            BuildGrid();
            UpdateCalendarData();
        }

        private string GetVietnameseDayOfWeek(DateTime dt)
        {
            switch (dt.DayOfWeek)
            {
                case DayOfWeek.Monday: return "Thứ Hai";
                case DayOfWeek.Tuesday: return "Thứ Ba";
                case DayOfWeek.Wednesday: return "Thứ Tư";
                case DayOfWeek.Thursday: return "Thứ Năm";
                case DayOfWeek.Friday: return "Thứ Sáu";
                case DayOfWeek.Saturday: return "Thứ Bảy";
                case DayOfWeek.Sunday: return "Chủ Nhật";
                default: return "";
            }
        }

        private async void TxtColumnName_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;
            string newName = tb.Text.Trim();
            if (string.IsNullOrWhiteSpace(newName)) return;

            if (tb.DataContext is KanbanTaskViewModel taskVm)
            {
                taskVm.IsEditingTitle = false;
                taskVm.TaskName = newName;
                try
                {
                    DateTime? utcDue = taskVm.DueDate.HasValue ? DateTime.SpecifyKind(taskVm.DueDate.Value, DateTimeKind.Local).ToUniversalTime() : null;
                    var payload = new
                    {
                        TaskId = taskVm.TaskId,
                        TaskName = taskVm.TaskName,
                        Description = taskVm.Description,
                        AssigneeId = taskVm.AssigneeId,
                        DueDate = utcDue,
                        Tags = taskVm.Tags,
                        ColumnId = taskVm.ColumnId,
                        OrderIndex = taskVm.OrderIndex,
                        IsActive = true
                    };
                    await _apiService.UpdateTaskAsync(taskVm.TaskId, payload);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi đổi tên: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else if (tb.DataContext is KanbanColumnViewModel vm)
            {
                vm.IsEditingTitle = false;
                vm.ColumnName = newName;
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

        public void RenderCalendar(KanbanBoardViewModel boardVm)
        {
            _boardVm = boardVm;
            UpdateCalendarData();
        }

        private void UpdateModeButtons()
        {
            if (_viewMode == CalendarViewMode.Day)
            {
                btnViewDay.Background = PrimaryBrush;
                btnViewDay.Foreground = Brushes.White;
                btnViewWeek.Background = Brushes.Transparent;
                btnViewWeek.Foreground = TextMutedBrush;
                lblCalendarHeader.Text = $"{GetVietnameseDayOfWeek(_selectedDate)}, {_selectedDate:dd/MM}";
            }
            else
            {
                btnViewWeek.Background = PrimaryBrush;
                btnViewWeek.Foreground = Brushes.White;
                btnViewDay.Background = Brushes.Transparent;
                btnViewDay.Foreground = TextMutedBrush;
                lblCalendarHeader.Text = $"Tuần ({_viewModel.CurrentWeekStart:dd/MM} - {_viewModel.CurrentWeekStart.AddDays(6):dd/MM})";
            }
        }

        private void BuildGrid()
        {
            CalendarGrid.Children.Clear();
            CalendarGrid.RowDefinitions.Clear();
            CalendarGrid.ColumnDefinitions.Clear();
            _viewModel.Cells.Clear();

            UpdateModeButtons();

            if (_viewMode == CalendarViewMode.Day)
            {
                BuildDayGrid();
            }
            else
            {
                BuildWeekGrid();
            }
        }

        /// <summary>
        /// Xây dựng giao diện xem 1 ngày trực quan theo chuẩn Trello (cột giờ bên trái, slot rộng toàn màn hình bên phải)
        /// </summary>
        private void BuildDayGrid()
        {
            CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) }); // Cột giờ
            CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Slot cả ngày

            // Header ngày banner
            CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            // Cell header giờ
            var headerHourBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Padding = new Thickness(6, 10, 6, 10)
            };
            headerHourBorder.Child = new TextBlock
            {
                Text = "GMT+7",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(headerHourBorder, 0);
            Grid.SetColumn(headerHourBorder, 0);
            CalendarGrid.Children.Add(headerHourBorder);

            // Banner ngày (với badge ngày tròn nổi bật như Trello)
            var headerDayBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(14, 8, 14, 8)
            };
            var headerStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            headerStack.Children.Add(new TextBlock
            {
                Text = GetVietnameseDayOfWeek(_selectedDate),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            });
            var dayBadge = new Border
            {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(13),
                Background = PrimaryBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            dayBadge.Child = new TextBlock
            {
                Text = _selectedDate.Day.ToString(),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            headerStack.Children.Add(dayBadge);
            headerDayBorder.Child = headerStack;
            Grid.SetRow(headerDayBorder, 0);
            Grid.SetColumn(headerDayBorder, 1);
            CalendarGrid.Children.Add(headerDayBorder);

            // Các khung giờ từ 07:00 đến 21:00 (15 khung giờ)
            int startHour = 7;
            int totalHours = 15;

            for (int r = 1; r <= totalHours; r++)
            {
                int hour = startHour + r - 1;
                CalendarGrid.RowDefinitions.Add(new RowDefinition { MinHeight = 68, Height = GridLength.Auto });

                // Nhãn giờ
                var timeBorder = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Padding = new Thickness(4, 10, 4, 0)
                };
                timeBorder.Child = new TextBlock
                {
                    Text = $"{hour:D2}:00",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetRow(timeBorder, r);
                Grid.SetColumn(timeBorder, 0);
                CalendarGrid.Children.Add(timeBorder);

                // Slot công việc
                var cellDate = DateTime.SpecifyKind(_selectedDate.Date.AddHours(hour), DateTimeKind.Local);
                var cellVm = new CalendarCellViewModel { SlotTime = cellDate };
                _viewModel.Cells.Add(cellVm);

                var itemsControl = new ItemsControl
                {
                    ItemsSource = cellVm.Tasks,
                    Margin = new Thickness(6, 3, 6, 3),
                    AllowDrop = true,
                    ItemTemplate = TaskTemplate,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    MinHeight = 56,
                    Background = Brushes.Transparent,
                    Tag = cellVm
                };

                GongSolutions.Wpf.DragDrop.DragDrop.SetIsDropTarget(itemsControl, true);
                GongSolutions.Wpf.DragDrop.DragDrop.SetDropHandler(itemsControl, this);
                GongSolutions.Wpf.DragDrop.DragDrop.SetIsDragSource(itemsControl, true);

                var slotBorder = new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Background = Brushes.White,
                    Child = itemsControl,
                    Tag = cellVm,
                    AllowDrop = true
                };

                GongSolutions.Wpf.DragDrop.DragDrop.SetIsDropTarget(slotBorder, true);
                GongSolutions.Wpf.DragDrop.DragDrop.SetDropHandler(slotBorder, this);

                Grid.SetRow(slotBorder, r);
                Grid.SetColumn(slotBorder, 1);
                CalendarGrid.Children.Add(slotBorder);
            }
        }

        /// <summary>
        /// Xây dựng giao diện xem 7 ngày trong tuần
        /// </summary>
        private void BuildWeekGrid()
        {
            CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            for (int i = 0; i < 7; i++)
            {
                CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition { MinWidth = 110, Width = new GridLength(1, GridUnitType.Star) });
            }

            CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            AddCellToGrid("Giờ / Ngày", 0, 0, true);
            for (int i = 0; i < 7; i++)
            {
                var dayDate = _viewModel.CurrentWeekStart.AddDays(i);
                string dayName = i == 6 ? "Chủ nhật" : $"Thứ {i + 2}";
                AddCellToGrid($"{dayName}\n{dayDate:dd/MM}", 0, i + 1, true);
            }

            int startHour = 8;
            int totalHours = 10;
            for (int row = 1; row <= totalHours; row++)
            {
                CalendarGrid.RowDefinitions.Add(new RowDefinition { MinHeight = 90, Height = GridLength.Auto });
                int hour = startHour + row - 1;
                AddCellToGrid($"{hour:D2}:00", row, 0, true);

                for (int col = 1; col <= 7; col++)
                {
                    var cellDate = DateTime.SpecifyKind(_viewModel.CurrentWeekStart.AddDays(col - 1).AddHours(hour), DateTimeKind.Local);
                    var cellVm = new CalendarCellViewModel { SlotTime = cellDate };
                    _viewModel.Cells.Add(cellVm);

                    var itemsControl = new ItemsControl
                    {
                        ItemsSource = cellVm.Tasks,
                        Margin = new Thickness(4),
                        AllowDrop = true,
                        ItemTemplate = TaskTemplate,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        MinHeight = 70,
                        Background = Brushes.Transparent,
                        Tag = cellVm
                    };

                    GongSolutions.Wpf.DragDrop.DragDrop.SetIsDropTarget(itemsControl, true);
                    GongSolutions.Wpf.DragDrop.DragDrop.SetDropHandler(itemsControl, this);
                    GongSolutions.Wpf.DragDrop.DragDrop.SetIsDragSource(itemsControl, true);

                    var border = new Border
                    {
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                        BorderThickness = new Thickness(0, 0, 1, 1),
                        Background = Brushes.White,
                        Child = itemsControl,
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
        }

        private void AddCellToGrid(string text, int row, int col, bool isHeader)
        {
            var border = new Border
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Background = isHeader ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")) : Brushes.White,
                Padding = new Thickness(6, 8, 6, 8)
            };
            
            var textBlock = new TextBlock
            {
                Text = text,
                FontSize = 12,
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

            // Gán các thẻ có DueDate vào cell phù hợp; các thẻ hoàn toàn chưa có DueDate vào Unscheduled
            foreach (var column in _boardVm.Columns)
            {
                // 1. Quét các công việc cụ thể (Tasks) bên trong cột
                foreach (var task in column.Tasks)
                {
                    if (task.DueDate.HasValue)
                    {
                        DateTime rawDue = task.DueDate.Value;
                        DateTime due = rawDue.Kind == DateTimeKind.Utc 
                            ? rawDue.ToLocalTime() 
                            : rawDue;

                        var cell = _viewModel.Cells.FirstOrDefault(c => c.SlotTime.Date == due.Date && c.SlotTime.Hour == due.Hour);
                        if (cell == null)
                        {
                            if (_viewMode == CalendarViewMode.Day && due.Date == _selectedDate.Date)
                            {
                                int clampedHour = Math.Clamp(due.Hour, 7, 21);
                                cell = _viewModel.Cells.FirstOrDefault(c => c.SlotTime.Date == due.Date && c.SlotTime.Hour == clampedHour);
                            }
                            else if (_viewMode == CalendarViewMode.Week && due.Date >= _viewModel.CurrentWeekStart.Date && due.Date < _viewModel.CurrentWeekStart.AddDays(7).Date)
                            {
                                int clampedHour = Math.Clamp(due.Hour, 8, 17);
                                cell = _viewModel.Cells.FirstOrDefault(c => c.SlotTime.Date == due.Date && c.SlotTime.Hour == clampedHour);
                            }
                        }

                        if (cell != null)
                        {
                            if (!cell.Tasks.Contains(task))
                            {
                                cell.Tasks.Add(task);
                            }
                        }
                        // LƯU Ý: Task đã có DueDate nhưng không thuộc ngày/tuần đang xem thì KHÔNG BAO GIỜ thêm vào Chưa xếp lịch
                    }
                    else
                    {
                        // Chỉ những task hoàn toàn chưa có DueDate mới hiển thị ở cột Chưa xếp lịch
                        if (!_viewModel.UnscheduledTasks.Contains(task))
                        {
                            _viewModel.UnscheduledTasks.Add(task);
                        }
                    }
                }

                // 2. Quét bản thân cột nếu cột có DueDate
                if (column.DueDate.HasValue)
                {
                    DateTime rawDue = column.DueDate.Value;
                    DateTime due = rawDue.Kind == DateTimeKind.Utc 
                        ? rawDue.ToLocalTime() 
                        : rawDue;

                    var cell = _viewModel.Cells.FirstOrDefault(c => c.SlotTime.Date == due.Date && c.SlotTime.Hour == due.Hour);
                    if (cell == null)
                    {
                        if (_viewMode == CalendarViewMode.Day && due.Date == _selectedDate.Date)
                        {
                            int clampedHour = Math.Clamp(due.Hour, 7, 21);
                            cell = _viewModel.Cells.FirstOrDefault(c => c.SlotTime.Date == due.Date && c.SlotTime.Hour == clampedHour);
                        }
                        else if (_viewMode == CalendarViewMode.Week && due.Date >= _viewModel.CurrentWeekStart.Date && due.Date < _viewModel.CurrentWeekStart.AddDays(7).Date)
                        {
                            int clampedHour = Math.Clamp(due.Hour, 8, 17);
                            cell = _viewModel.Cells.FirstOrDefault(c => c.SlotTime.Date == due.Date && c.SlotTime.Hour == clampedHour);
                        }
                    }

                    if (cell != null && !cell.Tasks.Contains(column))
                    {
                        cell.Tasks.Add(column);
                    }
                }
            }
        }

        private void BtnViewDay_Click(object sender, RoutedEventArgs e)
        {
            _viewMode = CalendarViewMode.Day;
            BuildGrid();
            UpdateCalendarData();
        }

        private void BtnViewWeek_Click(object sender, RoutedEventArgs e)
        {
            _viewMode = CalendarViewMode.Week;
            BuildGrid();
            UpdateCalendarData();
        }

        private void Prev_Click(object sender, RoutedEventArgs e)
        {
            _isUpdatingDate = true;
            if (_viewMode == CalendarViewMode.Day)
            {
                _selectedDate = _selectedDate.AddDays(-1);
                _viewModel.CurrentWeekStart = GetStartOfWeek(_selectedDate);
            }
            else
            {
                _selectedDate = _selectedDate.AddDays(-7);
                _viewModel.CurrentWeekStart = _viewModel.CurrentWeekStart.AddDays(-7);
            }
            dtpSelectedDate.SelectedDate = _selectedDate;
            _isUpdatingDate = false;
            BuildGrid();
            UpdateCalendarData();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            _isUpdatingDate = true;
            if (_viewMode == CalendarViewMode.Day)
            {
                _selectedDate = _selectedDate.AddDays(1);
                _viewModel.CurrentWeekStart = GetStartOfWeek(_selectedDate);
            }
            else
            {
                _selectedDate = _selectedDate.AddDays(7);
                _viewModel.CurrentWeekStart = _viewModel.CurrentWeekStart.AddDays(7);
            }
            dtpSelectedDate.SelectedDate = _selectedDate;
            _isUpdatingDate = false;
            BuildGrid();
            UpdateCalendarData();
        }

        private void Today_Click(object sender, RoutedEventArgs e)
        {
            _isUpdatingDate = true;
            _selectedDate = DateTime.Today;
            _viewModel.CurrentWeekStart = GetStartOfWeek(_selectedDate);
            dtpSelectedDate.SelectedDate = _selectedDate;
            _isUpdatingDate = false;
            BuildGrid();
            UpdateCalendarData();
        }

        private void DtpSelectedDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingDate) return;
            if (dtpSelectedDate.SelectedDate.HasValue)
            {
                _selectedDate = dtpSelectedDate.SelectedDate.Value;
                _viewModel.CurrentWeekStart = GetStartOfWeek(_selectedDate);
                BuildGrid();
                UpdateCalendarData();
            }
        }

        private async void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = sender as ComboBox;
            if (cb == null || !cb.IsLoaded) return;

            string? newStatus = e.AddedItems.Count > 0 ? e.AddedItems[0] as string : cb.SelectedValue as string;
            if (string.IsNullOrWhiteSpace(newStatus)) return;

            if (cb.DataContext is KanbanTaskViewModel taskVm)
            {
                taskVm.Status = newStatus;
                if (_boardVm != null)
                {
                    var targetCol = _boardVm.Columns.FirstOrDefault(c => c.ColumnName.Equals(newStatus, StringComparison.OrdinalIgnoreCase) || c.Status.Equals(newStatus, StringComparison.OrdinalIgnoreCase));
                    if (targetCol != null && targetCol.ColumnId != taskVm.ColumnId)
                    {
                        var oldCol = _boardVm.Columns.FirstOrDefault(c => c.Tasks.Contains(taskVm));
                        if (oldCol != null)
                        {
                            oldCol.Tasks.Remove(taskVm);
                            taskVm.ColumnId = targetCol.ColumnId;
                            targetCol.Tasks.Add(taskVm);
                            await _apiService.UpdateTaskColumnAsync(taskVm.TaskId, targetCol.ColumnId);
                        }
                    }
                }
            }
            else if (cb.DataContext is KanbanColumnViewModel columnVm)
            {
                columnVm.Status = newStatus;
                await UpdateColumnApiAsync(columnVm);
            }
        }

        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.Data is KanbanTaskViewModel || dropInfo.Data is KanbanColumnViewModel)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.Effects = DragDropEffects.Move;
            }
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            var item = dropInfo.Data as ViewModelBase;
            if (item == null) return;

            CalendarCellViewModel? targetCell = null;
            System.Collections.ObjectModel.ObservableCollection<ViewModelBase>? targetList = null;

            // 1. Phân giải targetCell hoặc Unscheduled qua cây VisualTree
            DependencyObject? current = dropInfo.VisualTarget as DependencyObject;
            while (current != null)
            {
                if (current is FrameworkElement fe)
                {
                    if (fe.Tag as string == "Unscheduled" || fe == txtUnscheduledHeader || fe == icUnscheduledTasks)
                    {
                        targetList = _viewModel.UnscheduledTasks;
                        break;
                    }

                    if (fe.Tag is CalendarCellViewModel cell)
                    {
                        targetCell = cell;
                        targetList = cell.Tasks;
                        break;
                    }

                    if (fe is ItemsControl ic && ic.ItemsSource is System.Collections.ObjectModel.ObservableCollection<ViewModelBase> colList)
                    {
                        if (colList == _viewModel.UnscheduledTasks)
                        {
                            targetList = _viewModel.UnscheduledTasks;
                            break;
                        }
                        targetCell = _viewModel.Cells.FirstOrDefault(c => c.Tasks == colList);
                        if (targetCell != null)
                        {
                            targetList = targetCell.Tasks;
                            break;
                        }
                    }
                }
                current = VisualTreeHelper.GetParent(current);
            }

            // 2. Dự phòng: dropInfo.TargetCollection
            if (targetList == null)
            {
                if (dropInfo.TargetCollection == _viewModel.UnscheduledTasks)
                {
                    targetList = _viewModel.UnscheduledTasks;
                }
                else if (dropInfo.TargetCollection is System.Collections.ObjectModel.ObservableCollection<ViewModelBase> targetCol)
                {
                    targetCell = _viewModel.Cells.FirstOrDefault(c => c.Tasks == targetCol);
                    if (targetCell != null) targetList = targetCell.Tasks;
                }
            }

            // 3. Dự phòng toạ độ bên trong CalendarGrid khi nhả chuột trúng vùng trống / viền ngoài của lưới lịch
            if (targetCell == null && targetList == null)
            {
                try
                {
                    Point dropPos = dropInfo.DropPosition;
                    if (dropInfo.VisualTarget is Visual v)
                    {
                        var screenPt = v.PointToScreen(dropPos);
                        dropPos = CalendarGrid.PointFromScreen(screenPt);
                    }

                    foreach (UIElement child in CalendarGrid.Children)
                    {
                        if (child is FrameworkElement fe && fe.Tag is CalendarCellViewModel cell)
                        {
                            Point childPos = fe.TranslatePoint(new Point(0, 0), CalendarGrid);
                            Rect bounds = new Rect(childPos, fe.RenderSize);
                            if (bounds.Contains(dropPos))
                            {
                                targetCell = cell;
                                targetList = cell.Tasks;
                                break;
                            }
                        }
                    }

                    if (targetCell == null)
                    {
                        if (_viewMode == CalendarViewMode.Day)
                        {
                            double headerHeight = CalendarGrid.RowDefinitions.Count > 0 ? CalendarGrid.RowDefinitions[0].ActualHeight : 40;
                            if (headerHeight <= 0) headerHeight = 40;
                            double gridRemainingHeight = Math.Max(100, CalendarGrid.ActualHeight - headerHeight);
                            double rowHeight = gridRemainingHeight / 15.0;
                            int rowHour = (int)((dropPos.Y - headerHeight) / rowHeight) + 7;
                            rowHour = Math.Clamp(rowHour, 7, 21);

                            targetCell = _viewModel.Cells.FirstOrDefault(c => c.SlotTime.Date == _selectedDate.Date && c.SlotTime.Hour == rowHour);
                            if (targetCell != null) targetList = targetCell.Tasks;
                        }
                        else
                        {
                            double timeColWidth = CalendarGrid.ColumnDefinitions.Count > 0 ? CalendarGrid.ColumnDefinitions[0].ActualWidth : 64;
                            if (timeColWidth <= 0) timeColWidth = 64;
                            double gridRemainingWidth = Math.Max(100, CalendarGrid.ActualWidth - timeColWidth);
                            double colWidth = gridRemainingWidth / 7.0;
                            int dayCol = (int)((dropPos.X - timeColWidth) / colWidth);
                            dayCol = Math.Clamp(dayCol, 0, 6);

                            double headerHeight = CalendarGrid.RowDefinitions.Count > 0 ? CalendarGrid.RowDefinitions[0].ActualHeight : 40;
                            if (headerHeight <= 0) headerHeight = 40;
                            double gridRemainingHeight = Math.Max(100, CalendarGrid.ActualHeight - headerHeight);
                            double rowHeight = gridRemainingHeight / 10.0;
                            int rowHour = (int)((dropPos.Y - headerHeight) / rowHeight) + 8;
                            rowHour = Math.Clamp(rowHour, 8, 17);

                            DateTime targetDate = _viewModel.CurrentWeekStart.AddDays(dayCol).Date;
                            targetCell = _viewModel.Cells.FirstOrDefault(c => c.SlotTime.Date == targetDate && c.SlotTime.Hour == rowHour);
                            if (targetCell != null) targetList = targetCell.Tasks;
                        }
                    }
                }
                catch
                {
                    // Bỏ qua lỗi tính toán tọa độ
                }
            }

            if (targetList == null) return;

            // 4. Tìm nguồn (sourceList)
            var sourceList = (dropInfo.DragInfo?.SourceCollection as System.Collections.ObjectModel.ObservableCollection<ViewModelBase>)
                ?? (_viewModel.UnscheduledTasks.Contains(item)
                    ? _viewModel.UnscheduledTasks
                    : _viewModel.Cells.FirstOrDefault(c => c.Tasks.Contains(item))?.Tasks);

            if (sourceList != null)
            {
                if (sourceList != targetList)
                {
                    sourceList.Remove(item);
                    int insertIndex = Math.Min(Math.Max(0, dropInfo.InsertIndex), targetList.Count);
                    targetList.Insert(insertIndex, item);

                    if (targetCell != null)
                    {
                        SaveItemDueDate(item, targetCell.SlotTime);
                    }
                    else if (targetList == _viewModel.UnscheduledTasks)
                    {
                        ClearItemDueDate(item);
                    }
                }
                else
                {
                    int oldIndex = sourceList.IndexOf(item);
                    int insertIndex = Math.Min(Math.Max(0, dropInfo.InsertIndex), sourceList.Count - 1);
                    if (oldIndex != insertIndex && oldIndex >= 0 && insertIndex >= 0)
                    {
                        sourceList.Move(oldIndex, insertIndex);
                    }
                }
            }
            else
            {
                foreach (var cell in _viewModel.Cells)
                {
                    if (cell.Tasks != targetList && cell.Tasks.Contains(item))
                    {
                        cell.Tasks.Remove(item);
                    }
                }
                if (_viewModel.UnscheduledTasks != targetList && _viewModel.UnscheduledTasks.Contains(item))
                {
                    _viewModel.UnscheduledTasks.Remove(item);
                }

                if (!targetList.Contains(item))
                {
                    int insertIndex = Math.Min(Math.Max(0, dropInfo.InsertIndex), targetList.Count);
                    targetList.Insert(insertIndex, item);
                }

                if (targetCell != null)
                {
                    SaveItemDueDate(item, targetCell.SlotTime);
                }
                else if (targetList == _viewModel.UnscheduledTasks)
                {
                    ClearItemDueDate(item);
                }
            }
        }

        private void ClearItemDueDate(ViewModelBase item)
        {
            if (item is KanbanTaskViewModel taskVm)
            {
                taskVm.DueDate = null;
                _ = SaveTaskDueDateApiAsync(taskVm);
            }
            else if (item is KanbanColumnViewModel colVm)
            {
                colVm.DueDate = null;
                _ = UpdateColumnApiAsync(colVm);
            }
        }

        private void SaveItemDueDate(ViewModelBase item, DateTime slotTime)
        {
            DateTime newSlotTime = DateTime.SpecifyKind(slotTime, DateTimeKind.Local);
            if (item is KanbanTaskViewModel taskVm)
            {
                taskVm.DueDate = newSlotTime;
                _ = SaveTaskDueDateApiAsync(taskVm);
            }
            else if (item is KanbanColumnViewModel colVm)
            {
                colVm.DueDate = newSlotTime;
                _ = UpdateColumnApiAsync(colVm);
            }
        }

        private async Task SaveTaskDueDateApiAsync(KanbanTaskViewModel taskVm)
        {
            try
            {
                DateTime? dueDateUtc = null;
                if (taskVm.DueDate.HasValue)
                {
                    var dt = taskVm.DueDate.Value;
                    DateTime localDt = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
                    localDt = DateTime.SpecifyKind(localDt, DateTimeKind.Local);
                    dueDateUtc = localDt.ToUniversalTime();
                }

                var payload = new
                {
                    TaskId = taskVm.TaskId,
                    TaskName = taskVm.TaskName,
                    Description = taskVm.Description,
                    AssigneeId = taskVm.AssigneeId,
                    DueDate = dueDateUtc,
                    Tags = taskVm.Tags,
                    ColumnId = taskVm.ColumnId,
                    OrderIndex = taskVm.OrderIndex,
                    IsActive = true
                };
                await _apiService.UpdateTaskAsync(taskVm.TaskId, payload);
            }
            catch
            {
                // Đồng bộ chạy nền không chặn UI
            }
        }

        private async Task UpdateColumnApiAsync(KanbanColumnViewModel updatedVm)
        {
            try
            {
                DateTime? dueDateUtc = null;
                if (updatedVm.DueDate.HasValue)
                {
                    var dt = updatedVm.DueDate.Value;
                    DateTime localDt = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
                    localDt = DateTime.SpecifyKind(localDt, DateTimeKind.Local);
                    dueDateUtc = localDt.ToUniversalTime();
                }

                var payload = new
                {
                    ColumnId = updatedVm.ColumnId,
                    ColumnName = updatedVm.ColumnName,
                    OrderIndex = updatedVm.OrderIndex,
                    ProjectId = updatedVm.ProjectId,
                    DueDate = dueDateUtc,
                    Status = string.IsNullOrWhiteSpace(updatedVm.Status) ? "Vừa giao" : updatedVm.Status,
                    IsActive = true
                };
                await _apiService.UpdateColumnAsync(updatedVm.ColumnId, payload);
            }
            catch
            {
                // Đồng bộ chạy nền không chặn UI
            }
        }
    }
}