using System.Windows;
using System.Windows.Input;

namespace AgileTaskManager.Desktop
{
    public partial class DashboardWindow : Window
    {
        public DashboardWindow()
        {
            InitializeComponent();
        }

        // Mở cửa sổ tạo mới (Nút Menu bên trái)
        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            CreateWindow createWindow = new CreateWindow();
            createWindow.Show();
        }

        // 1. Ẩn nút, hiện ô nhập Title
        private void BtnShowAddList_Click(object sender, MouseButtonEventArgs e)
        {
            btnShowAddList.Visibility = Visibility.Collapsed;
            panelAddListInput.Visibility = Visibility.Visible;
            txtNewListName.Focus();
        }

        // 2. Hủy tạo danh sách
        private void BtnCancelAddList_Click(object sender, RoutedEventArgs e)
        {
            txtNewListName.Text = "";
            panelAddListInput.Visibility = Visibility.Collapsed;
            btnShowAddList.Visibility = Visibility.Visible;
        }

        // 3. BỘ NÃO ĐÚC CỘT KANBAN:
        private void AddNewList()
        {
            string title = txtNewListName.Text.Trim();
            if (string.IsNullOrEmpty(title)) return;

            // Đúc 1 cái Cột mới toanh từ Khuôn (UserControl) và truyền Title vào
            KanbanColumn newColumn = new KanbanColumn(title);

            // Chèn cột mới này vào vị trí kế cuối (Ngay đằng trước khu vực nhập list mới)
            spBoard.Children.Insert(spBoard.Children.Count - 1, newColumn);

            // Xóa trắng chữ và đóng Form lại
            txtNewListName.Text = "";
            panelAddListInput.Visibility = Visibility.Collapsed;
            btnShowAddList.Visibility = Visibility.Visible;

            // Ép tiêu điểm bay về nút "Thêm danh sách khác" để triệt tiêu hoàn toàn khung nét đứt
            btnShowAddList.Focus();
        }

        // 4. Bấm nút Lưu
        private void BtnConfirmAddList_Click(object sender, RoutedEventArgs e)
        {
            AddNewList();
        }

        // 5. Bấm Enter để Lưu
        private void TxtNewListName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddNewList();
                e.Handled = true;
                Keyboard.ClearFocus();
            }
        }
    }
}