# Agile Task Manager

Ứng dụng quản lý công việc theo mô hình Kanban, gồm 2 phần tách biệt:

- **AgileTaskManagerAPI** — Backend REST API viết bằng **ASP.NET Core 8** + **Entity Framework Core** (SQL Server). Đã tích hợp bảo mật toàn diện với xác thực JWT và mã hóa BCrypt.
- **AgileTaskManager.Desktop** — Ứng dụng desktop **WPF (.NET 8)** gọi trực tiếp API. Cung cấp giao diện bảng Kanban mượt mà với chức năng Drag & Drop tuỳ chỉnh (Custom Animation Canvas).

## ✨ Tính năng nổi bật

- **Bảo mật (Security & Auth):**
  - Đăng ký và Đăng nhập với mật khẩu được mã hóa an toàn bằng thuật toán `BCrypt`.
  - Cấp phát và xác thực bằng `JSON Web Token (JWT)`.
  - Giới hạn quyền truy cập API khắt khe bằng `[Authorize]`.
  - Chính sách CORS bảo mật (chỉ cho phép các Origin chỉ định).
- **Quản lý User & Project**:
  - Tạo user mới và tạo dự án gắn với `OwnerId`.
- **Bảng Kanban tuỳ chỉnh (Desktop App)**:
  - Hiển thị công việc theo từng cột trạng thái.
  - Hỗ trợ thao tác kéo-thả (Drag & Drop) mượt mà bằng kỹ thuật Overlay Canvas tự code (nghiêng thẻ, đổ bóng, dịch chuyển mượt mà thẻ bị lướt qua).
- **Giao diện Desktop**:
  - `LoginWindow` — màn hình đăng nhập nhận Token JWT.
  - `DashboardWindow` — bảng Kanban chính.
  - `KanbanColumn` — UserControl đại diện cho một cột Kanban và xử lý logic Animation.
  - Tự động đính kèm `Bearer Token` vào mọi request qua `AppConfig.Client`.
- **Trang Admin Web (`wwwroot/admin.html`)**:
  - Công cụ web tối giản giúp Dev tạo nhanh Dữ liệu (User, Project, Task).
  - Tích hợp đăng nhập JWT và hiển thị toàn bộ Database dưới dạng 3 bảng Data Tables tiện dụng.

## 🛠 Công nghệ sử dụng

| Thành phần | Công nghệ |
|---|---|
| Backend | ASP.NET Core 8 Web API, EF Core 8 (SQL Server), BCrypt.Net-Next, JWT Bearer |
| Desktop | WPF (.NET 8), Custom Canvas Animation, HttpClient |
| API Docs | Swagger/Swashbuckle |

## 📁 Cấu trúc thư mục

```
AgileTaskManagerAPI/
├── AgileTaskManager.Desktop/       # Ứng dụng WPF
│   ├── LoginWindow.xaml(.cs)       # Màn hình đăng nhập JWT
│   ├── MainWindow.xaml(.cs)        # Màn hình dự phòng
│   ├── DashboardWindow.xaml(.cs)   # Bảng Kanban (Chứa DragOverlayCanvas)
│   ├── KanbanColumn.xaml(.cs)      # Component xử lý Drag & Drop
│   └── AppConfig.cs                # Nơi chứa HttpClient dùng chung & Token
│
├── AgileTaskManagerAPI/            # Backend Web API
│   ├── Controllers/
│   │   ├── AuthController.cs       # Đăng nhập & cấp Token
│   │   ├── UsersController.cs
│   │   ├── ProjectsController.cs
│   │   ├── ColumnsController.cs
│   │   └── TasksController.cs
│   ├── Model/
│   │   ├── LoginRequest.cs         # DTO đăng nhập
│   │   ├── User.cs                 # Entity User
│   │   ├── Project.cs
│   │   ├── KanbanColumn.cs
│   │   └── AppTask.cs
│   ├── Data/AppDbContext.cs
│   ├── wwwroot/                    # Công cụ admin.html
│   ├── appsettings.json            # Chứa JWT Key & CorsSettings
│   └── Program.cs
│
└── AgileTaskManagerAPI.slnx
```

## 🗄 Mô hình dữ liệu

- **User**: `UserId`, `Username`, `PasswordHash` (BCrypt), `Email` (unique)
- **Project**: `ProjectId`, `ProjectName`, `Description`, `CreatedAt`, `OwnerId` → `User`
- **KanbanColumn**: `ColumnId`, `ColumnName`, `Position`, `ProjectId` → `Project`
- **AppTask**: `TaskId`, `TaskName`, `Description`, `ColumnId` → `KanbanColumn`, `ProjectId` → `Project`, `AssigneeId` → `User` (có thể null)

## 🚀 Cài đặt & chạy thử

### Yêu cầu

- .NET SDK 8.0+
- SQL Server (LocalDB / SQL Express / Server đầy đủ đều được)
- Visual Studio 2022+ (khuyến nghị) hoặc .NET CLI

### 1. Cấu hình Database & Security

Sửa chuỗi kết nối trong `AgileTaskManagerAPI/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=<TÊN_SERVER>;Database=AgileTaskManagerDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

*File này cũng chứa cấu hình bảo mật `JwtSettings` và `CorsSettings`. Bạn có thể thay đổi các origin hợp lệ trong mảng `AllowedOrigins`.*

### 2. Chạy migrations & khởi động API

```bash
cd AgileTaskManagerAPI
dotnet restore
dotnet ef database update
dotnet run
```
API sẽ chạy tại `http://localhost:5279`. 
Mở trình duyệt truy cập `http://localhost:5279/admin.html` để tạo nhanh dữ liệu test (hoặc test API tại `http://localhost:5279/swagger`).

### 3. Chạy ứng dụng Desktop

Mở `AgileTaskManagerAPI.slnx` bằng Visual Studio → chọn **AgileTaskManager.Desktop** làm Startup Project → **F5**.
App sẽ mở lên màn hình Login, nhập tài khoản bạn vừa tạo bên `admin.html` để vào bảng Kanban.

## 📡 API Endpoints (Yêu cầu JWT Token)

Đa số các API dưới đây đều yêu cầu Header: `Authorization: Bearer <TOKEN>`

| Method | Endpoint | Mô tả | Trạng thái |
|---|---|---|---|
| POST | `/api/Users/register` | Đăng ký user mới | Public |
| POST | `/api/Auth/login` | Xác thực, trả về JWT Token | Public |
| GET | `/api/Users` | Lấy danh sách user | Khóa |
| GET | `/api/Projects` | Lấy danh sách project | Khóa |
| POST | `/api/Projects` | Tạo project mới | Khóa |
| GET | `/api/Tasks` | Lấy toàn bộ task | Khóa |
| GET | `/api/Tasks/project/{id}`| Lấy task của 1 project | Khóa |
| POST | `/api/Tasks` | Tạo task mới | Khóa |
| PATCH | `/api/Tasks/{id}/column`| Cập nhật cột (Drag-Drop) | Khóa |

## 🗺 Roadmap / TODO (Đã hoàn thành!)

- [x] Mã hoá mật khẩu (BCrypt)
- [x] Xác thực & phân quyền (JWT Authentication)
- [x] Thắt chặt CORS Policy cho production
- [x] Kéo-thả (drag & drop) custom mượt mà trên Desktop
- [x] Công cụ Web Dashboard Data Management
- [ ] Trang Team Directory / Files trên Desktop (Sắp tới)

## 🤝 Đóng góp

Pull request và issue đều được hoan nghênh.
