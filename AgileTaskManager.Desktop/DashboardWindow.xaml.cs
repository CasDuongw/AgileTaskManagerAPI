using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace AgileTaskManager.Desktop
{
    public partial class DashboardWindow : Window
    {
        public DashboardWindow()
        {
            InitializeComponent();
        }

        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            CreateWindow createWindow = new CreateWindow();
            createWindow.Show();
        }

        private void BtnShowAddCard_Click(object sender, RoutedEventArgs e)
        {
            btnShowAddCard.Visibility = Visibility.Collapsed;
            panelAddCardInput.Visibility = Visibility.Visible;
            txtNewTaskName.Focus();
        }

        private void BtnCancelAddCard_Click(object sender, RoutedEventArgs e)
        {
            txtNewTaskName.Text = "";
            panelAddCardInput.Visibility = Visibility.Collapsed;
            btnShowAddCard.Visibility = Visibility.Visible;
        }

        // BỘ NÃO XỬ LÝ DỮ LIỆU ĐẦU VÀO
        private void AddNewTask()
        {
            string inputContent = txtNewTaskName.Text.Trim();
            if (string.IsNullOrEmpty(inputContent)) return;

            // TRƯỜNG HỢP 1: NẾU DANH SÁCH CHƯA CÓ GÌ -> NHẬP TITLE
            if (spTaskList.Children.Count == 0)
            {
                // 1. Tạo một Grid chứa Tiêu đề (bên trái) và nút X (bên phải)
                Grid titleGrid = new Grid();
                titleGrid.Margin = new Thickness(5, 5, 5, 15);
                titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // -- Chữ Tiêu đề in đậm
                TextBlock title = new TextBlock
                {
                    Text = inputContent,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#172B4D")),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(title, 0);

                // -- Nút Xóa toàn bộ cột (Mặc định ẩn, chờ rê chuột)
                Button btnDeleteList = new Button
                {
                    Content = "✕",
                    Foreground = Brushes.Gray,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    FontSize = 14,
                    Cursor = Cursors.Hand,
                    Visibility = Visibility.Hidden,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right, // Ép về bên phải
                    Margin = new Thickness(0, 0, 10, 0), // Đẩy lùi vào trong 10 pixel để cách viền
                    Padding = new Thickness(5)
                };
                Grid.SetColumn(btnDeleteList, 1);

                // -- Hiệu ứng Hover: Rê chuột vào vùng Tiêu đề thì hiện nút X
                titleGrid.MouseEnter += (s, e) => btnDeleteList.Visibility = Visibility.Visible;
                titleGrid.MouseLeave += (s, e) => btnDeleteList.Visibility = Visibility.Hidden;

                // -- Sự kiện XÓA: Reset toàn bộ ảo thuật
                btnDeleteList.Click += (s, e) =>
                {
                    // Xóa sạch Tiêu đề và mọi Task bên trong
                    spTaskList.Children.Clear();

                    // Trả lại tên nút mặc định
                    lblShowBtnText.Text = "Thêm danh sách";
                    btnConfirmAdd.Content = "Thêm danh sách";

                    // Giấu nút bên phải đi (Tạo cảm giác nút lùi về vị trí cũ)
                    btnAddNewColumn.Visibility = Visibility.Collapsed;
                };

                // Nhét chữ và nút X vào Grid, rồi nhét Grid vào Cột
                titleGrid.Children.Add(title);
                titleGrid.Children.Add(btnDeleteList);
                spTaskList.Children.Add(titleGrid);

                // 2. Đổi nhãn các nút nhập liệu thành "Thêm thẻ"
                lblShowBtnText.Text = "Thêm thẻ";
                btnConfirmAdd.Content = "Thêm thẻ";

                // 3. Hiện nút "Thêm danh sách khác" mọc ra bên phải
                btnAddNewColumn.Visibility = Visibility.Visible;
            }
            // TRƯỜNG HỢP 2: TỪ LẦN NHẬP THỨ 2 -> TẠO TASK CÓ CHECKBOX VÀ NÚT XÓA
            else
            {
                Border newCard = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(8), // Bo góc mềm mại hơn
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 10),
                    Effect = new DropShadowEffect { BlurRadius = 4, ShadowDepth = 1, Color = (Color)ColorConverter.ConvertFromString("#000000"), Opacity = 0.1, Direction = 270 }
                };

                Grid cardGrid = new Grid();
                cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                CheckBox chk = new CheckBox
                {
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#172B4D"),
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    Content = new TextBlock { Text = inputContent, TextWrapping = TextWrapping.Wrap }
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
                    Visibility = Visibility.Hidden, // Ẩn chờ Hover
                    VerticalAlignment = VerticalAlignment.Top,
                    Padding = new Thickness(5, 0, 0, 0)
                };
                Grid.SetColumn(btnDelete, 1);

                // Sự kiện Di chuột & Xóa
                newCard.MouseEnter += (s, e) => btnDelete.Visibility = Visibility.Visible;
                newCard.MouseLeave += (s, e) => btnDelete.Visibility = Visibility.Hidden;
                btnDelete.Click += (s, e) => spTaskList.Children.Remove(newCard);

                cardGrid.Children.Add(chk);
                cardGrid.Children.Add(btnDelete);
                newCard.Child = cardGrid;

                spTaskList.Children.Add(newCard);
            }

            // Xóa chữ để nhập liên tục
            txtNewTaskName.Text = "";
            txtNewTaskName.Focus();
        }

        private void BtnConfirmAddCard_Click(object sender, RoutedEventArgs e)
        {
            AddNewTask();
        }

        private void TxtNewTaskName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift) return;
                AddNewTask();
                e.Handled = true;
            }
        }
    }
}