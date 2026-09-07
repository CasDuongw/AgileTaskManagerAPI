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

namespace AgileTaskManager.Desktop
{
    public partial class KanbanColumn : UserControl
    {
        // Lấy URL cấu hình từ AppConfig
        // Đã xóa biến local ApiBaseUrl hardcode
        private static readonly HttpClient client = AppConfig.Client;

        // [MỚI] Biến lưu trữ ID của dự án cho cột này
        private int _currentProjectId;
        public int ColumnId { get; private set; }

        // Khi một cột được tạo ra, nó yêu cầu phải truyền Tiêu đề, ID dự án và ID cột
        public KanbanColumn(string title, int projectId, int columnId)
        {
            InitializeComponent();
            lblTitle.Text = title;
            _currentProjectId = projectId;
            ColumnId = columnId;

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


        // Lệnh xóa nguyên cái Cột Kanban này khỏi bảng
        private void BtnDeleteColumn_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra an toàn: Nếu Parent đúng là một Panel thì mới xóa
            if (this.Parent is Panel parentPanel)
            {
                parentPanel.Children.Remove(this);

                // Tìm màn hình Dashboard và ra lệnh cập nhật lại chữ
                Window window = Window.GetWindow(this);
                if (window is DashboardWindow dashboard)
                {
                    dashboard.UpdateAddListButtonText();
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

        // [CŨ] Khai báo hàm lúc trước là: private void AddNewTask()
        // [MỚI] Đổi thành async void để hỗ trợ chờ phản hồi từ API
        private async void AddNewTask()
        {
            string taskContent = txtNewTaskName.Text.Trim();
            if (string.IsNullOrEmpty(taskContent)) return;

            // ==========================================
            // [MỚI] BƯỚC 1: CHUẨN BỊ VÀ GỬI DỮ LIỆU LÊN API
            // ==========================================
            var newTask = new
            {
                taskName = taskContent,
                projectId = this._currentProjectId, // Đã sửa: Lấy ID thật của project đang mở thay vì gán cứng số 8

                columnId = this.ColumnId // Đã sửa: Sử dụng ColumnId thay cho string Status
            };

            try
            {
                // Gọi API POST để lưu dữ liệu thẳng vào SQL Server
                var response = await client.PostAsJsonAsync($"{AppConfig.ApiBaseUrl}/Tasks", newTask);

                // Nếu API trả về thành công (HTTP 200/201) -> Tiến hành vẽ Task lên màn hình
                if (response.IsSuccessStatusCode)
                {
                    var createdTask = await response.Content.ReadFromJsonAsync<TaskResponse>();

                    // ==========================================
                    // [CŨ] BƯỚC 2: VẼ GIAO DIỆN (Nằm trọn trong khối thành công)
                    // (Đây là toàn bộ đoạn code cũ của bạn, giữ nguyên không đổi 1 chữ)
                    // ==========================================
                    Border newCard = new Border
                    {
                        Background = Brushes.White,
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(12),
                        Margin = new Thickness(0, 0, 0, 8),
                        BorderThickness = new Thickness(1),
                        BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#E2E8F0"),
                        FocusVisualStyle = null,
                        AllowDrop = true,
                        Tag = createdTask?.taskId
                    };

                    newCard.Effect = new DropShadowEffect { BlurRadius = 4, ShadowDepth = 1, Opacity = 0.1 };
                    TransformGroup tg = new TransformGroup();
                    tg.Children.Add(new TranslateTransform());
                    tg.Children.Add(new RotateTransform { Angle = 0, CenterX = 100, CenterY = 25 });
                    newCard.RenderTransform = tg;

                    Grid cardGrid = new Grid();
                    cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    CheckBox chk = new CheckBox
                    {
                        Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#111827"),
                        FontWeight = FontWeights.SemiBold,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Content = new TextBlock { Text = taskContent, TextWrapping = TextWrapping.Wrap }
                    };
                    Grid.SetColumn(chk, 0);

                    Button btnDelete = new Button
                    {
                        Content = "\xE711",
                        FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                        Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#64748B"),
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        FontSize = 14,
                        Cursor = Cursors.Hand,
                        Visibility = Visibility.Hidden,
                        VerticalAlignment = VerticalAlignment.Top,
                        Padding = new Thickness(5, 0, 0, 0)
                    };
                    Grid.SetColumn(btnDelete, 1);

                    newCard.PreviewMouseLeftButtonDown += Card_PreviewMouseLeftButtonDown;
                    newCard.PreviewMouseMove += Card_PreviewMouseMove;
                    newCard.PreviewMouseLeftButtonUp += Card_PreviewMouseLeftButtonUp;

                    newCard.MouseEnter += (s, e) => btnDelete.Visibility = Visibility.Visible;
                    newCard.MouseLeave += (s, e) => btnDelete.Visibility = Visibility.Hidden;

                    // Nút xóa UI (Lưu ý: Tạm thời mới chỉ xóa giao diện, chưa xóa dưới DB)
                    btnDelete.Click += (s, e) => spTaskList.Children.Remove(newCard);

                    cardGrid.Children.Add(chk);
                    cardGrid.Children.Add(btnDelete);
                    newCard.Child = cardGrid;

                    spTaskList.Children.Add(newCard);

                    // ==========================================
                    // [CŨ] BƯỚC 3: DỌN DẸP Ô NHẬP LIỆU
                    // ==========================================
                    txtNewTaskName.Text = "";
                    txtNewTaskName.Focus();
                }
                else
                {
                    // [MỚI] Xử lý khi API báo lỗi (VD: sai ID, sai cấu trúc)
                    string errorMsg = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi khi lưu Task vào Database!\nChi tiết: {errorMsg}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                // [MỚI] Xử lý khi API không phản hồi (chưa bật backend)
                MessageBox.Show($"Lỗi kết nối API: {ex.Message}\nBạn đã chạy backend chưa?", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==============================================================================
        // [MỚI] HÀM CÔNG KHAI ĐỂ DASHBOARD "BƠM" TASK TỪ DATABASE VÀO CỘT
        // ==============================================================================
        public void AddTaskCard(int taskId, string taskContent)
        {
            Border newCard = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8),
                BorderThickness = new Thickness(1),
                BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#E2E8F0"),
                FocusVisualStyle = null,
                AllowDrop = true,

                // Cực kỳ quan trọng: Gắn TaskId từ Database vào túi bí mật (Tag) của thẻ
                // Để sau này kéo thả mình biết đang kéo cái thẻ nào!
                Tag = taskId
            };

            newCard.Effect = new DropShadowEffect { BlurRadius = 4, ShadowDepth = 1, Opacity = 0.1 };
            TransformGroup tg = new TransformGroup();
            tg.Children.Add(new TranslateTransform());
            tg.Children.Add(new RotateTransform { Angle = 0, CenterX = 100, CenterY = 25 });
            newCard.RenderTransform = tg;

            Grid cardGrid = new Grid();
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            CheckBox chk = new CheckBox
            {
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#111827"),
                FontWeight = FontWeights.SemiBold,
                VerticalContentAlignment = VerticalAlignment.Center,
                Content = new TextBlock { Text = taskContent, TextWrapping = TextWrapping.Wrap }
            };
            Grid.SetColumn(chk, 0);

            Button btnDelete = new Button
            {
                Content = "\xE711",
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#64748B"),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 14,
                Cursor = Cursors.Hand,
                Visibility = Visibility.Hidden,
                VerticalAlignment = VerticalAlignment.Top,
                Padding = new Thickness(5, 0, 0, 0)
            };
            Grid.SetColumn(btnDelete, 1);

            // Gắn sự kiện kéo thả cho thẻ
            newCard.PreviewMouseLeftButtonDown += Card_PreviewMouseLeftButtonDown;
            newCard.PreviewMouseMove += Card_PreviewMouseMove;
            newCard.PreviewMouseLeftButtonUp += Card_PreviewMouseLeftButtonUp;

            // Sự kiện hiện nút xóa khi hover
            newCard.MouseEnter += (s, e) => btnDelete.Visibility = Visibility.Visible;
            newCard.MouseLeave += (s, e) => btnDelete.Visibility = Visibility.Hidden;

            // Xóa UI khi bấm nút (Bước sau sẽ cập nhật xóa DB sau)
            btnDelete.Click += (s, e) => spTaskList.Children.Remove(newCard);

            cardGrid.Children.Add(chk);
            cardGrid.Children.Add(btnDelete);
            newCard.Child = cardGrid;

            // Bơm thẻ vào giao diện cột
            spTaskList.Children.Add(newCard);
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
        // TÍNH NĂNG DRAG & DROP (KÉO THẢ CỘT)
        // ==============================================================================

        // Biến lưu trữ tọa độ ban đầu khi click chuột xuống
        private Point _dragStartPoint;

        // 1. Khi nhấn chuột trái vào Tiêu đề
        private void TitleGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Lưu lại vị trí chính xác của con trỏ chuột lúc vừa nhấn
            _dragStartPoint = e.GetPosition(null);
        }

        // 2. Khi di chuyển chuột (Trong lúc vẫn đang giữ chuột trái)
        private void TitleGrid_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // Nếu không giữ chuột trái thì thôi, không làm gì cả
            if (e.LeftButton != MouseButtonState.Pressed) return;

            // Tính toán xem chuột đã kéo đi được một quãng bao xa so với lúc nhấn
            Point mousePos = e.GetPosition(null);
            Vector diff = _dragStartPoint - mousePos;

            // Nếu kéo đủ xa (vượt qua mức chống rung tay mặc định của Windows)
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                // Bắt đầu gói cái cột này (this) lại và kích hoạt chế độ Kéo (DragDropEffects.Move)
                DragDrop.DoDragDrop(this, this, DragDropEffects.Move);
            }
        }

        // 3. Khi có một vật thể đang lơ lửng bay ngang qua cái Cột này
        private void UserControl_DragOver(object sender, DragEventArgs e)
        {
            // Kiểm tra: Nếu vật đang bay tới ĐÚNG LÀ MỘT CỘT KANBAN, và KHÔNG PHẢI CHÍNH NÓ
            if (e.Data.GetDataPresent(typeof(KanbanColumn)) && e.Data.GetData(typeof(KanbanColumn)) != this)
            {
                e.Effects = DragDropEffects.Move; // Bật đèn xanh, cho phép thả
            }
            else
            {
                e.Effects = DragDropEffects.None; // Bật đèn đỏ, cấm thả
            }
            e.Handled = true;
        }

        // 4. Khi người dùng buông tay THẢ cái cột kia xuống cái cột này
        private void UserControl_Drop(object sender, DragEventArgs e)
        {
            // Bắt lấy cái Cột đang bị thả xuống
            if (e.Data.GetDataPresent(typeof(KanbanColumn)))
            {
                KanbanColumn droppedColumn = e.Data.GetData(typeof(KanbanColumn)) as KanbanColumn; // Kẻ xâm nhập
                KanbanColumn targetColumn = this; // Chủ nhà (cột đang bị đè lên)

                // Đảm bảo không tự thả lên chính mình
                if (droppedColumn != null && droppedColumn != targetColumn)
                {
                    // Tìm cái "Bảng" (StackPanel) đang chứa cả 2 anh em
                    if (this.Parent is Panel parentPanel)
                    {
                        // Tìm số thứ tự (Index) của cả 2
                        int targetIndex = parentPanel.Children.IndexOf(targetColumn);

                        // Rút cái cột đang kéo ra khỏi Bảng
                        parentPanel.Children.Remove(droppedColumn);

                        // Chèn nó lại vào đúng vị trí của Chủ nhà
                        parentPanel.Children.Insert(targetIndex, droppedColumn);

                        // [MỚI] Đồng bộ thứ tự mới lên Server
                        SyncColumnOrderToServer(parentPanel);
                    }
                }
            }
        }

        private async void SyncColumnOrderToServer(Panel parentPanel)
        {
            var reorderList = new List<object>();
            int order = 0;

            foreach (UIElement child in parentPanel.Children)
            {
                if (child is KanbanColumn col)
                {
                    reorderList.Add(new { ColumnId = col.ColumnId, OrderIndex = order });
                    order++;
                }
            }

            try
            {
                await client.PutAsJsonAsync($"{AppConfig.ApiBaseUrl}/Columns/reorder", reorderList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi đồng bộ thứ tự cột: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ==============================================================================
        // TÍNH NĂNG DRAG & DROP (ANIMATION TRELLO) CHO CÁC THẺ TASK NHỎ
        // ==============================================================================
        private static bool _isCardDragging = false;
        private static Point _cardClickPosition;
        private static Border _draggedCard;
        private static Border _placeholder;
        private static StackPanel _originalPanel;
        private static int _originalIndex;
        private static KanbanColumn _sourceColumn;

        private void Card_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Button || e.OriginalSource is DependencyObject obj && FindParent<Button>(obj) != null)
                return;

            _draggedCard = sender as Border;
            _cardClickPosition = e.GetPosition(_draggedCard);
            _draggedCard.CaptureMouse();
        }

        private void Card_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedCard == null || e.LeftButton != MouseButtonState.Pressed) return;

            Point currentPos = e.GetPosition(_draggedCard);
            if (!_isCardDragging && (Math.Abs(currentPos.X - _cardClickPosition.X) > SystemParameters.MinimumHorizontalDragDistance ||
                                     Math.Abs(currentPos.Y - _cardClickPosition.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                StartCardDrag();
            }

            if (_isCardDragging)
            {
                DashboardWindow dashboard = Window.GetWindow(this) as DashboardWindow;
                if (dashboard == null) return;
                
                Canvas overlay = dashboard.FindName("DragOverlayCanvas") as Canvas;
                if (overlay == null) return;

                Point mousePosInCanvas = e.GetPosition(overlay);
                Canvas.SetLeft(_draggedCard, mousePosInCanvas.X - _cardClickPosition.X);
                Canvas.SetTop(_draggedCard, mousePosInCanvas.Y - _cardClickPosition.Y);

                UpdateCardPlaceholderPosition(mousePosInCanvas, dashboard);
            }
        }

        private void StartCardDrag()
        {
            _isCardDragging = true;
            _originalPanel = VisualTreeHelper.GetParent(_draggedCard) as StackPanel;
            if (_originalPanel == null) return;
            
            _sourceColumn = FindParent<KanbanColumn>(_originalPanel);
            _originalIndex = _originalPanel.Children.IndexOf(_draggedCard);

            _placeholder = new Border
            {
                Width = _draggedCard.ActualWidth,
                Height = _draggedCard.ActualHeight,
                Margin = _draggedCard.Margin,
                Background = new SolidColorBrush(Color.FromArgb(50, 150, 150, 150)),
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(Colors.LightGray),
                BorderThickness = new Thickness(1)
            };

            DashboardWindow dashboard = Window.GetWindow(this) as DashboardWindow;
            Canvas overlay = dashboard.FindName("DragOverlayCanvas") as Canvas;

            Point absolutePos = _draggedCard.TransformToAncestor(overlay).Transform(new Point(0, 0));
            _originalPanel.Children.Remove(_draggedCard);
            _originalPanel.Children.Insert(_originalIndex, _placeholder);

            overlay.Children.Add(_draggedCard);
            Canvas.SetLeft(_draggedCard, absolutePos.X);
            Canvas.SetTop(_draggedCard, absolutePos.Y);
            Panel.SetZIndex(_draggedCard, 9999);

            if (_draggedCard.RenderTransform is TransformGroup tg && tg.Children.Count > 1)
            {
                if (tg.Children[1] is RotateTransform rotateTransform)
                {
                    DoubleAnimation tiltAnim = new DoubleAnimation(4, TimeSpan.FromMilliseconds(150));
                    rotateTransform.BeginAnimation(RotateTransform.AngleProperty, tiltAnim);
                }
            }

            if (_draggedCard.Effect is DropShadowEffect shadow)
            {
                shadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, new DoubleAnimation(15, TimeSpan.FromMilliseconds(150)));
                shadow.BeginAnimation(DropShadowEffect.ShadowDepthProperty, new DoubleAnimation(5, TimeSpan.FromMilliseconds(150)));
                shadow.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(0.3, TimeSpan.FromMilliseconds(150)));
            }
        }

        private void UpdateCardPlaceholderPosition(Point mousePosInCanvas, DashboardWindow dashboard)
        {
            var hitTestResult = VisualTreeHelper.HitTest(dashboard, mousePosInCanvas);
            if (hitTestResult == null) return;

            KanbanColumn targetColumn = FindParent<KanbanColumn>(hitTestResult.VisualHit);
            if (targetColumn == null) return;

            StackPanel targetPanel = targetColumn.FindName("spTaskList") as StackPanel;
            if (targetPanel == null) return;

            int newIndex = -1;
            Canvas overlay = dashboard.FindName("DragOverlayCanvas") as Canvas;

            for (int i = 0; i < targetPanel.Children.Count; i++)
            {
                UIElement child = targetPanel.Children[i];
                if (child == _placeholder) continue;

                Point childPos = child.TransformToAncestor(overlay).Transform(new Point(0, 0));
                if (mousePosInCanvas.Y > childPos.Y && mousePosInCanvas.Y < childPos.Y + child.RenderSize.Height)
                {
                    newIndex = mousePosInCanvas.Y < childPos.Y + child.RenderSize.Height / 2 ? i : i + 1;
                    break;
                }
            }

            if (newIndex == -1) newIndex = targetPanel.Children.Count;

            StackPanel currentPlaceholderPanel = VisualTreeHelper.GetParent(_placeholder) as StackPanel;
            if (currentPlaceholderPanel != targetPanel || newIndex != targetPanel.Children.IndexOf(_placeholder))
            {
                if (currentPlaceholderPanel != null)
                {
                    currentPlaceholderPanel.Children.Remove(_placeholder);
                }
                
                AnimateCardsSlide(targetPanel, newIndex);

                if (newIndex >= targetPanel.Children.Count)
                    targetPanel.Children.Add(_placeholder);
                else
                    targetPanel.Children.Insert(newIndex, _placeholder);
            }
        }

        private void AnimateCardsSlide(StackPanel targetPanel, int newIndex)
        {
            int currentIndex = targetPanel.Children.IndexOf(_placeholder);
            
            foreach (UIElement child in targetPanel.Children)
            {
                if (child == _placeholder) continue;

                if (child.RenderTransform is TransformGroup tg && tg.Children.Count > 0)
                {
                    if (tg.Children[0] is TranslateTransform translate)
                    {
                        translate.BeginAnimation(TranslateTransform.YProperty, null);
                        
                        DoubleAnimation slideAnim = new DoubleAnimation
                        {
                            To = 0,
                            Duration = TimeSpan.FromMilliseconds(200),
                            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                        };

                        int childIndex = targetPanel.Children.IndexOf(child);
                        if (currentIndex == -1)
                        {
                            if (childIndex >= newIndex) translate.Y = -_placeholder.ActualHeight;
                        }
                        else 
                        {
                            if (childIndex == newIndex && newIndex < currentIndex) translate.Y = -_placeholder.ActualHeight;
                            else if (childIndex == newIndex - 1 && newIndex > currentIndex) translate.Y = _placeholder.ActualHeight;
                        }

                        if (translate.Y != 0)
                            translate.BeginAnimation(TranslateTransform.YProperty, slideAnim);
                    }
                }
            }
        }

        private void Card_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggedCard != null)
            {
                _draggedCard.ReleaseMouseCapture();
            }

            if (!_isCardDragging) 
            {
                _draggedCard = null;
                return;
            }

            DashboardWindow dashboard = Window.GetWindow(this) as DashboardWindow;
            Canvas overlay = dashboard?.FindName("DragOverlayCanvas") as Canvas;
            if (overlay == null)
            {
                FinishDrop();
                return;
            }

            Point targetPos = _placeholder.TransformToAncestor(overlay).Transform(new Point(0, 0));

            DoubleAnimation snapXAnim = new DoubleAnimation(targetPos.X, TimeSpan.FromMilliseconds(150)) { EasingFunction = new QuadraticEase() };
            DoubleAnimation snapYAnim = new DoubleAnimation(targetPos.Y, TimeSpan.FromMilliseconds(150)) { EasingFunction = new QuadraticEase() };

            snapYAnim.Completed += (s, ev) => FinishDrop();

            _draggedCard.BeginAnimation(Canvas.LeftProperty, snapXAnim);
            _draggedCard.BeginAnimation(Canvas.TopProperty, snapYAnim);
        }

        private void FinishDrop()
        {
            if (_draggedCard.RenderTransform is TransformGroup tg && tg.Children.Count > 1)
            {
                if (tg.Children[1] is RotateTransform rotateTransform)
                {
                    rotateTransform.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(100)));
                }
            }

            if (_draggedCard.Effect is DropShadowEffect shadow)
            {
                shadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, new DoubleAnimation(4, TimeSpan.FromMilliseconds(100)));
                shadow.BeginAnimation(DropShadowEffect.ShadowDepthProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(100)));
                shadow.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(0.1, TimeSpan.FromMilliseconds(100)));
            }

            DashboardWindow dashboard = Window.GetWindow(this) as DashboardWindow;
            Canvas overlay = dashboard?.FindName("DragOverlayCanvas") as Canvas;
            if (overlay != null)
            {
                overlay.Children.Remove(_draggedCard);
                _draggedCard.BeginAnimation(Canvas.LeftProperty, null);
                _draggedCard.BeginAnimation(Canvas.TopProperty, null);
            }

            StackPanel targetPanel = VisualTreeHelper.GetParent(_placeholder) as StackPanel;
            if (targetPanel != null)
            {
                int dropIndex = targetPanel.Children.IndexOf(_placeholder);
                targetPanel.Children.Remove(_placeholder);
                targetPanel.Children.Insert(dropIndex, _draggedCard);
                
                KanbanColumn targetColumn = FindParent<KanbanColumn>(targetPanel);
                if (targetColumn != _sourceColumn)
                {
                    targetColumn.SyncCardStatusToServer(_draggedCard, _originalPanel, _originalIndex);
                }
            }
            else
            {
                _originalPanel.Children.Insert(_originalIndex, _draggedCard);
            }

            _isCardDragging = false;
            _draggedCard = null;
            _placeholder = null;
        }

        private async void SyncCardStatusToServer(Border card, Panel oldParent, int oldIndex)
        {
            if (card?.Tag is not int taskId) return;

            try
            {
                var response = await client.PatchAsJsonAsync($"{AppConfig.ApiBaseUrl}/Tasks/{taskId}/column", this.ColumnId);
                if (!response.IsSuccessStatusCode)
                {
                    string errorMsg = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Kéo thả xịt rồi (Server từ chối)!\nChi tiết: {errorMsg}\nThẻ sẽ được bế về chỗ cũ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    RollbackCardMove(card, oldParent, oldIndex);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Rớt mạng hoặc Server sập rồi!\nChi tiết: {ex.Message}\nThẻ sẽ được bế về chỗ cũ cho chắc cú.", "Lỗi Hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
                RollbackCardMove(card, oldParent, oldIndex);
            }
        }

        private void RollbackCardMove(Border card, Panel oldParent, int oldIndex)
        {
            if (card.Parent is Panel currentParent)
            {
                currentParent.Children.Remove(card);
            }
            
            if (oldParent != null)
            {
                if (oldIndex >= 0 && oldIndex <= oldParent.Children.Count)
                {
                    oldParent.Children.Insert(oldIndex, card);
                }
                else
                {
                    oldParent.Children.Add(card); 
                }
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
