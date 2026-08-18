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


            // [GIẢI QUYẾT VẤN ĐỀ 2] Đợi UI vẽ xong form mới ép Focus
            Dispatcher.BeginInvoke(new System.Action(() => txtNewListName.Focus()));
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

            // Cập nhật lại chữ sau khi thêm cột thành công
            UpdateAddListButtonText();
        }

        // [CODE MỚI]: Hàm kiểm tra và đổi chữ tự động
        public void UpdateAddListButtonText()
        {
            // Nếu có nhiều hơn 1 phần tử (Nghĩa là có cột + cái form thêm list)
            if (spBoard.Children.Count > 1)
            {
                lblAddListText.Text = "Thêm danh sách khác";
            }
            else
            {
                lblAddListText.Text = "Thêm danh sách";
            }
        }

        // 4. Bấm nút Lưu
        private void BtnConfirmAddList_Click(object sender, RoutedEventArgs e)
        {
            AddNewList();
        }

        // 5. Bấm Enter để Lưu
        private void TxtNewListName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // [GIẢI QUYẾT VẤN ĐỀ 3] Bấm ESC -> Hủy tạo danh sách
            if (e.Key == Key.Escape)
            {
                BtnCancelAddList_Click(null, null);
                e.Handled = true;
                return;
            }

            // Bấm Enter -> Lưu danh sách
            if (e.Key == Key.Enter)
            {
                AddNewList();
                e.Handled = true;
                // (Vì tạo cột xong thì cái form Thêm danh sách sẽ tự trượt sang phải và đóng lại, nên không cần ép Focus lại vào đây)
            }
        }
    }
}