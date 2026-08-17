# Agile Task Manager

Ứng dụng quản lý công việc theo mô hình Kanban, gồm 2 phần tách biệt:

- **AgileTaskManagerAPI** — Backend REST API viết bằng **ASP.NET Core 8** + **Entity Framework Core** (SQL Server).
- **AgileTaskManager.Desktop** — Ứng dụng desktop **WPF (.NET 8)** dùng **MahApps.Metro** + **MaterialDesignInXaml** làm giao diện, gọi trực tiếp vào API ở trên qua `HttpClient`.

## ✨ Tính năng

- **Quản lý User**: đăng ký tài khoản (`POST /api/Users/register`), xem danh sách user (`GET /api/Users`)
- **Quản lý Project**: tạo dự án mới, xem danh sách dự án (gắn với `OwnerId`)
- **Quản lý Task theo bảng Kanban**:
  - Tạo task gắn với một Project, có thể giao (`Assignee`) cho một User
  - Task mặc định ở trạng thái `ToDo`
  - Cập nhật trạng thái task qua `PATCH /api/Tasks/{id}/status` (`ToDo` → `InProgress` → `Done`) — dùng cho thao tác kéo-thả thẻ
  - Lấy danh sách task theo từng Project
- **Giao diện Desktop**:
  - `MainWindow` — màn hình đăng nhập/khởi động
  - `DashboardWindow` — bảng Kanban: thêm/xoá cột (danh sách), thêm thẻ task vào từng cột
  - `CreateWindow` — form tạo nhanh User / Project / Task
  - `KanbanColumn` — UserControl đại diện cho một cột Kanban
- **Trang admin web tối giản** (`wwwroot/admin.html`, `wwwroot/index.html`) phục vụ test nhanh API mà không cần mở app desktop

## 🛠 Công nghệ sử dụng

| Thành phần | Công nghệ |
|---|---|
| Backend | ASP.NET Core 8 Web API, Entity Framework Core 8 (SQL Server), Swagger/Swashbuckle |
| Desktop | WPF (.NET 8), MahApps.Metro, MaterialDesignThemes |
| CORS | Mở toàn bộ (`AllowAnyOrigin`) — chỉ dùng cho giai đoạn MVP/test |

## 📁 Cấu trúc thư mục

```
AgileTaskManagerAPI/
├── AgileTaskManager.Desktop/       # Ứng dụng WPF
│   ├── MainWindow.xaml(.cs)        # Màn hình chính
│   ├── DashboardWindow.xaml(.cs)   # Bảng Kanban
│   ├── CreateWindow.xaml(.cs)      # Form tạo User/Project/Task
│   └── KanbanColumn.xaml(.cs)      # UserControl 1 cột Kanban
│
├── AgileTaskManagerAPI/            # Backend Web API
│   ├── Controllers/
│   │   ├── UsersController.cs
│   │   ├── ProjectsController.cs
│   │   └── TasksController.cs
│   ├── Model/
│   │   ├── User.cs
│   │   ├── Project.cs
│   │   └── AppTask.cs
│   ├── Data/AppDbContext.cs
│   ├── Migrations/                 # EF Core migrations
│   ├── wwwroot/                    # admin.html, index.html (test nhanh qua trình duyệt)
│   └── Program.cs
│
└── AgileTaskManagerAPI.slnx
```

## 🗄 Mô hình dữ liệu

- **User**: `UserId`, `Username`, `PasswordHash`, `Email` (unique)
- **Project**: `ProjectId`, `ProjectName`, `Description`, `CreatedAt`, `OwnerId` → `User`
- **AppTask**: `TaskId`, `TaskName`, `Description`, `Status` (`ToDo` / `InProgress` / `Done`), `ProjectId` → `Project`, `AssigneeId` → `User` (có thể null)

## 🚀 Cài đặt & chạy thử

### Yêu cầu

- .NET SDK 8.0+
- SQL Server (LocalDB / SQL Express / Server đầy đủ đều được)
- Visual Studio 2022+ (khuyến nghị để mở `.slnx` và chạy WPF) hoặc .NET CLI

### 1. Cấu hình kết nối database

Sửa chuỗi kết nối trong `AgileTaskManagerAPI/appsettings.json` cho đúng với SQL Server trên máy bạn:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=<TÊN_SERVER>;Database=AgileTaskManagerDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

> ⚠️ Không commit connection string chứa mật khẩu thật lên GitHub. Nên dùng `Trusted_Connection` (Windows Auth) hoặc User Secrets thay vì `uid/pwd` cứng trong file.

### 2. Chạy migrations & khởi động API

```bash
cd AgileTaskManagerAPI
dotnet restore
dotnet ef database update   # tạo database theo Migrations có sẵn
dotnet run
```

API mặc định chạy tại `http://localhost:5279`, Swagger UI tại `http://localhost:5279/swagger`.

### 3. Chạy ứng dụng Desktop

Mở `AgileTaskManagerAPI.slnx` bằng Visual Studio → chọn **AgileTaskManager.Desktop** làm Startup Project → **F5**.

> Đảm bảo API đang chạy ở `http://localhost:5279` trước (địa chỉ này đang được hard-code trong `MainWindow.xaml.cs` và `CreateWindow.xaml.cs` qua biến `ApiBaseUrl`). Nếu đổi port, nhớ sửa lại ở cả 2 file.

## 📡 API Endpoints

| Method | Endpoint | Mô tả |
|---|---|---|
| POST | `/api/Users/register` | Đăng ký user mới |
| GET | `/api/Users` | Lấy danh sách user |
| GET | `/api/Projects` | Lấy danh sách project |
| POST | `/api/Projects` | Tạo project mới |
| GET | `/api/Tasks/project/{projectId}` | Lấy tất cả task của 1 project |
| POST | `/api/Tasks` | Tạo task mới |
| PATCH | `/api/Tasks/{id}/status` | Cập nhật trạng thái task |

## 🗺 Roadmap / TODO

- [ ] Mã hoá mật khẩu (hiện `PasswordHash` đang lưu plain text để test luồng MVP)
- [ ] Xác thực & phân quyền (JWT)
- [ ] Kéo-thả (drag & drop) task giữa các cột Kanban trên Desktop
- [ ] Trang Team Directory / Files trên Desktop
- [ ] Giới hạn CORS khi lên production (hiện đang `AllowAnyOrigin`)

## 🤝 Đóng góp

Pull request và issue đều được hoan nghênh.

## 📄 License

Chưa có license — thêm file `LICENSE` nếu muốn public chính thức (ví dụ MIT).
