using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AgileTaskManager.Desktop
{
    /// <summary>
    /// Interaction logic for DashboardWindow.xaml
    /// </summary>
    public partial class DashboardWindow : Window
    {
        public DashboardWindow()
        {
            InitializeComponent();
        }

        // Hàm gọi cửa sổ Tạo mới khi bấm nút Create trên Sidebar
        private void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            // Khởi tạo cửa sổ MainWindow (cửa sổ chứa 3 cột Tạo User/Project/Task của bạn)
            MainWindow createWindow = new MainWindow();

            // Hiển thị nó lên mà không tắt cửa sổ Dashboard hiện tại
            createWindow.Show();
        }
    }
}
