using ControlzEx.Standard;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace AgileTaskManager.Desktop
{
    public partial class KanbanColumn : UserControl
    {
        // Khi một cột được tạo ra, nó yêu cầu phải truyền Tiêu đề vào
        public KanbanColumn(string title)
        {
            InitializeComponent();
            lblTitle.Text = title;

            // Chặn việc UserControl tự bắt tiêu điểm bàn phím/chuột gây ra viền nét đứt
            IsTabStop = false;
            Focusable = false;

            // Hover chuột vào tiêu đề -> Hiện nút Xóa cột
            titleGrid.MouseEnter += (s, e) => btnDeleteColumn.Visibility = Visibility.Visible;
            titleGrid.MouseLeave += (s, e) => btnDeleteColumn.Visibility = Visibility.Hidden;
        }

        // Lệnh xóa nguyên cái Cột Kanban này khỏi bảng
        private void BtnDeleteColumn_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra an toàn: Nếu Parent đúng là một Panel thì mới xóa
            if (this.Parent is Panel parentPanel)
            {
                parentPanel.Children.Remove(this);
            }
        }

        // ----- CÁC LỆNH XỬ LÝ THÊM/XÓA THẺ TỪ TRƯỚC ĐẾN NAY -----
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

        private void AddNewTask()
        {
            string taskContent = txtNewTaskName.Text.Trim();
            if (string.IsNullOrEmpty(taskContent)) return;

            Border newCard = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10),
                FocusVisualStyle = null,
                Effect = new DropShadowEffect { BlurRadius = 4, ShadowDepth = 1, Color = (Color)ColorConverter.ConvertFromString("#000000"), Opacity = 0.1, Direction = 270 }
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

            newCard.MouseEnter += (s, e) => btnDelete.Visibility = Visibility.Visible;
            newCard.MouseLeave += (s, e) => btnDelete.Visibility = Visibility.Hidden;
            btnDelete.Click += (s, e) => spTaskList.Children.Remove(newCard);

            cardGrid.Children.Add(chk);
            cardGrid.Children.Add(btnDelete);
            newCard.Child = cardGrid;

            spTaskList.Children.Add(newCard);

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
                Keyboard.ClearFocus();
            }
        }
    }
}