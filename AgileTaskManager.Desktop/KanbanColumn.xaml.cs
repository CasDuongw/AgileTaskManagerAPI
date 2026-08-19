using ControlzEx.Standard;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Net.Http;
using System.Net.Http.Json;

namespace AgileTaskManager.Desktop
{
    public partial class KanbanColumn : UserControl
    {
        // Lấy URL cấu hình từ AppConfig
        // Đã xóa biến local ApiBaseUrl hardcode
        private static readonly HttpClient client = new HttpClient();

        // [MỚI] Biến lưu trữ ID của dự án cho cột này
        private int _currentProjectId;

        // Khi một cột được tạo ra, nó yêu cầu phải truyền Tiêu đề và ID dự án
        public KanbanColumn(string title, int projectId)
        {
            InitializeComponent();
            lblTitle.Text = title;
            _currentProjectId = projectId;

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
            public string status { get; set; }
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
                projectId = 8, // Tạm gán cứng vào Project số 1 để test luồng


                // Lấy trạng thái dựa vào tên cột (Ví dụ: "ToDo", "InProgress", "Done")
                // Lưu ý: Tên cột trên UI phải khớp với quy định trong Database
                status = this.lblTitle.Text
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
                        Padding = new Thickness(10),
                        Margin = new Thickness(0, 0, 0, 10),
                        FocusVisualStyle = null,
                        Effect = new DropShadowEffect { BlurRadius = 4, ShadowDepth = 1, Color = (Color)ColorConverter.ConvertFromString("#000000"), Opacity = 0.1, Direction = 270 },
                        AllowDrop = true,
                        Tag = createdTask?.taskId
                    };

                    Grid cardGrid = new Grid();
                    cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    CheckBox chk = new CheckBox
                    {
                        Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#172B4D"),
                        FontWeight = FontWeights.SemiBold,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Content = new TextBlock { Text = taskContent, TextWrapping = TextWrapping.Wrap }
                    };
                    Grid.SetColumn(chk, 0);

                    Button btnDelete = new Button
                    {
                        Content = "✕",
                        Foreground = Brushes.Gray,
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
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10),
                FocusVisualStyle = null,
                Effect = new DropShadowEffect { BlurRadius = 4, ShadowDepth = 1, Color = (Color)ColorConverter.ConvertFromString("#000000"), Opacity = 0.1, Direction = 270 },
                AllowDrop = true,

                // Cực kỳ quan trọng: Gắn TaskId từ Database vào túi bí mật (Tag) của thẻ
                // Để sau này kéo thả mình biết đang kéo cái thẻ nào!
                Tag = taskId
            };

            Grid cardGrid = new Grid();
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            CheckBox chk = new CheckBox
            {
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#172B4D"),
                FontWeight = FontWeights.SemiBold,
                VerticalContentAlignment = VerticalAlignment.Center,
                Content = new TextBlock { Text = taskContent, TextWrapping = TextWrapping.Wrap }
            };
            Grid.SetColumn(chk, 0);

            Button btnDelete = new Button
            {
                Content = "✕",
                Foreground = Brushes.Gray,
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
                    }
                }
            }
        }

        // ==============================================================================
        // TÍNH NĂNG DRAG & DROP CHO CÁC THẺ TASK NHỎ
        // ==============================================================================

        private Point _cardDragStartPoint;
        private int projectId;

        // 1. Khi nhấn chuột vào một thẻ Task
        private void Card_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Nếu bấm trúng nút Xóa (Button) thì không kích hoạt kéo, để người dùng còn kịp bấm xóa
            if (e.OriginalSource is Button || e.OriginalSource is DependencyObject obj && FindParent<Button>(obj) != null)
                return;

            _cardDragStartPoint = e.GetPosition(null);
        }

        // 2. Khi di chuột để nhấc thẻ Task lên
        private void Card_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            Point mousePos = e.GetPosition(null);
            Vector diff = _cardDragStartPoint - mousePos;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                Border draggedCard = sender as Border;
                if (draggedCard != null)
                {
                    // Đóng gói thẻ Task lại và kích hoạt lệnh kéo xuyên màn hình
                    DragDrop.DoDragDrop(draggedCard, draggedCard, DragDropEffects.Move);
                }
            }
        }

      
        private void ColumnBorder_DragOver(object sender, DragEventArgs e)
        {
            // Nếu vật thể đang kéo là một thẻ Task (thẻ Border) thì cho phép thả
            if (e.Data.GetDataPresent(typeof(Border)))
            {
                e.Effects = DragDropEffects.Move;
            }
            e.Handled = true;
        }

        private void ColumnBorder_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Border)))
            {
                Border droppedCard = e.Data.GetData(typeof(Border)) as Border;
                if (droppedCard != null)
                {
                    // 1. LẤY TỌA ĐỘ CHUỘT: Đo xem chuột đang nằm ở đâu so với toàn bộ danh sách Task
                    Point mousePos = e.GetPosition(spTaskList);

                    // Mặc định Index sẽ là ở cuối danh sách
                    int targetIndex = spTaskList.Children.Count;

                    // 2. DÙNG TOÁN HỌC TÌM VỊ TRÍ CHUẨN XÁC
                    // Quét qua từng thẻ Task đang có sẵn trong cột
                    for (int i = 0; i < spTaskList.Children.Count; i++)
                    {
                        UIElement child = spTaskList.Children[i];

                        // Bỏ qua chính cái thẻ mình đang cầm trên tay (Tránh tính toán sai lệch khi kéo trong cùng 1 cột)
                        if (child == droppedCard) continue;

                        // Tính tọa độ Y của thẻ này so với danh sách
                        Point childPos = child.TranslatePoint(new Point(0, 0), spTaskList);

                        // Nếu mũi tên chuột nằm cao hơn ĐIỂM GIỮA của thẻ hiện tại
                        // Nghĩa là người dùng muốn chèn lên trên thẻ này!
                        if (mousePos.Y < childPos.Y + (((FrameworkElement)child).ActualHeight / 2))
                        {
                            targetIndex = i; // Chốt hạ vị trí
                            break; // Dừng vòng lặp
                        }
                    }

                    // 3. TIẾN HÀNH RÚT - CẮM
                    Panel oldParent = droppedCard.Parent as Panel;
                    int oldIndex = -1; // [MỚI] Lưu lại vị trí cũ để phòng hờ rollback (Làm như một Senior lười)
                    if (oldParent != null)
                    {
                        oldIndex = oldParent.Children.IndexOf(droppedCard);

                        // Xử lý một cú lừa của Logic: Nếu bạn kéo thả trong CÙNG 1 CỘT, 
                        // khi rút thẻ cũ ra, các thẻ bên dưới sẽ bị giật lên 1 bậc làm sai số Index.
                        // Ta cần trừ đi 1 nấc nếu vị trí cũ nằm cao hơn vị trí mới.
                        if (oldParent == spTaskList && oldIndex < targetIndex)
                        {
                            targetIndex--;
                        }

                        // Rút khỏi cột cũ (hoặc vị trí cũ)
                        oldParent.Children.Remove(droppedCard);
                    }

                    // Cắm thẻ vào vị trí chuẩn không cần chỉnh
                    spTaskList.Children.Insert(targetIndex, droppedCard);

                    if (oldParent != spTaskList)
                        SyncCardStatusToServer(droppedCard, oldParent, oldIndex);
                }
            }
            e.Handled = true;
        }

        private async void SyncCardStatusToServer(Border card, Panel oldParent, int oldIndex)
        {
            if (card?.Tag is not int taskId) return;

            try
            {
                var response = await client.PatchAsJsonAsync($"{AppConfig.ApiBaseUrl}/Tasks/{taskId}/status", lblTitle.Text);
                if (!response.IsSuccessStatusCode)
                {
                    string errorMsg = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Kéo thả xịt rồi (Server từ chối)!\nChi tiết: {errorMsg}\nThẻ sẽ được bế về chỗ cũ.", "Lỗi Optimistic UI", MessageBoxButton.OK, MessageBoxImage.Error);
                    RollbackCardMove(card, oldParent, oldIndex);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Rớt mạng hoặc Server sập rồi!\nChi tiết: {ex.Message}\nThẻ sẽ được bế về chỗ cũ cho chắc cú.", "Lỗi Mạng/Hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
                RollbackCardMove(card, oldParent, oldIndex);
            }
        }

        // [MỚI] Bùa chú phục hồi nhân phẩm: Gọi khi API sập để kéo thẻ về nhà cũ
        private void RollbackCardMove(Border card, Panel oldParent, int oldIndex)
        {
            // 1. Nhổ thẻ ra khỏi chỗ vừa thả nhầm (ui giả dối)
            if (card.Parent is Panel currentParent)
            {
                currentParent.Children.Remove(card);
            }
            
            // 2. Tống nó về lại nhà cũ
            if (oldParent != null)
            {
                // Nhét lại đúng khe hở cũ
                if (oldIndex >= 0 && oldIndex <= oldParent.Children.Count)
                {
                    oldParent.Children.Insert(oldIndex, card);
                }
                else
                {
                    oldParent.Children.Add(card); // Backup nếu lỡ có gì đó kì quặc xảy ra
                }
            }
        }

        private void SpTaskList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Border)))
            {
                Border droppedCard = e.Data.GetData(typeof(Border)) as Border;
                if (droppedCard != null)
                {
                    Panel oldParent = droppedCard.Parent as Panel;
                    int oldIndex = -1; // [MỚI] Nhớ vị trí cũ trước khi nhổ thẻ lên
                    if (oldParent != null) 
                    {
                        oldIndex = oldParent.Children.IndexOf(droppedCard);
                    }

                    // Nếu thả vào khoảng không của chính cột đó thì không cần làm gì, hoặc rớt xuống cuối
                    if (oldParent == spTaskList) return;

                    // Rút khỏi cột cũ
                    if (oldParent != null) oldParent.Children.Remove(droppedCard);

                    // Thêm thẳng vào cuối danh sách của cột mới
                    spTaskList.Children.Add(droppedCard);

                    SyncCardStatusToServer(droppedCard, oldParent, oldIndex);
                }
            }
            e.Handled = true;
        }

        // Hàm phụ trợ dùng để kiểm tra xem người dùng có đang bấm hụt vào nút bấm hay không
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindParent<T>(parentObject);
        }
    }
}