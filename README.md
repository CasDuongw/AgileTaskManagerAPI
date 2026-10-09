# Agile Task Manager

Ứng dụng quản lý công việc theo mô hình Kanban, gồm 2 phần tách biệt:

- **AgileTaskManagerAPI** — Backend REST API viết bằng **ASP.NET Core 8** + **Entity Framework Core** (SQL Server). Đã tích hợp bảo mật toàn diện với xác thực JWT và phân quyền (RBAC).
- **AgileTaskManager.Desktop** — Ứng dụng desktop **WPF (.NET 8)** gọi trực tiếp API. Cung cấp giao diện bảng Kanban mượt mà với kiến trúc **MVVM (Model-View-ViewModel)** chuẩn mực, **Dependency Injection**, và thư viện **Gong WPF DragDrop**.

## ✨ Tính năng nổi bật

- **Bảo mật & Phân quyền (Security & RBAC):**
  - Đăng ký và Đăng nhập với mật khẩu được mã hóa an toàn bằng thuật toán `BCrypt`.
  - Cấp phát và xác thực bằng `JSON Web Token (JWT)`.
  - Giới hạn quyền truy cập API khắt khe bằng `[Authorize]`.
  - **Đặc quyền Admin:** Tài khoản `admin` có khả năng xem, thao tác (thêm/sửa/xóa/kéo thả) trên *toàn bộ* dự án của hệ thống.
  - **Quyền Nhân viên:** Người dùng bình thường chỉ được xem và thao tác trên dự án do chính họ tạo ra.

- **Quản lý User & Project (Role-based UI):**
  - Giao diện (UI) tự động thích ứng theo Role.
  - Admin có thể tạo User, Project cho bất kỳ ai, và tạo Task.
  - Nhân viên bị ẩn chức năng tạo User, các form giao diện tự động dàn lại bố cục sát lề trái. Nhân viên chỉ được gán Owner cho chính mình khi tạo Project.

- **Bảng Kanban & MVVM (Desktop App):**
  - Quản lý trạng thái bằng kiến trúc **MVVM 100%** (`ObservableCollection` và `INotifyPropertyChanged`).
  - Hệ thống **Dependency Injection (DI)** tập trung quản lý `ApiService` và các `ViewModels`, giúp ứng dụng tiêu thụ ít bộ nhớ.
  - Thay vì dùng Code-Behind lỗi thời, thao tác kéo-thả (Drag & Drop) được quản lý thông qua thư viện **gong-wpf-dragdrop**, liên kết chặt chẽ với ViewModel.

- **Trang Admin Web (`wwwroot/admin.html`)**:
  - Công cụ web tối giản giúp Dev tạo nhanh Dữ liệu (User, Project, Task).
  - Tích hợp đăng nhập JWT và hiển thị toàn bộ Database dưới dạng 3 bảng Data Tables tiện dụng.

## 🛠 Công nghệ sử dụng

| Thành phần | Công nghệ |
|---|---|
| Backend | ASP.NET Core 8 Web API, EF Core 8, BCrypt.Net-Next, JWT Bearer |
| Desktop | WPF (.NET 8), MVVM Pattern, Microsoft.Extensions.DependencyInjection, GongSolutions.WPF.DragDrop |
| API Docs | Swagger/Swashbuckle |

## 📁 Cấu trúc thư mục

```
AgileTaskManagerAPI/
├── AgileTaskManager.Desktop/       # Ứng dụng WPF
│   ├── App.xaml(.cs)               # Chứa cài đặt Dependency Injection (ServiceCollection)
│   ├── LoginWindow.xaml(.cs)       # Màn hình đăng nhập JWT
│   ├── DashboardWindow.xaml(.cs)   # Bảng Kanban (ẩn/hiện form theo Role)
│   ├── KanbanColumn.xaml(.cs)      # Component xử lý hiển thị Cột và Thẻ
│   ├── Services/                   
│   │   └── ApiService.cs           # Data Service Layer chuyên gọi REST API
│   ├── Commands/                   
│   │   └── RelayCommand.cs         # Adapter cho MVVM Button Binding
│   ├── ViewModels/                 # Các Model dùng cho MVVM (KanbanBoard, KanbanColumn, KanbanTask)
│   └── AppConfig.cs                # Nơi chứa config và CurrentUsername
│
├── AgileTaskManagerAPI/            # Backend Web API
│   ├── Controllers/
│   │   ├── AuthController.cs       # Đăng nhập & cấp Token
│   │   ├── UsersController.cs
│   │   ├── ProjectsController.cs   # API list (Có logic bypass cho Admin)
│   │   ├── ColumnsController.cs    # API cột (Có logic bypass cho Admin)
│   │   └── TasksController.cs      # API thẻ (Có logic bypass cho Admin)
│   ├── Model/
│   │   └── (User.cs, Project.cs, KanbanColumn.cs, AppTask.cs, LoginRequest.cs)
│   ├── Data/AppDbContext.cs
│   ├── wwwroot/                    # Công cụ admin.html
│   └── appsettings.json            # Chứa JWT Key & CorsSettings
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
Mở trình duyệt truy cập `http://localhost:5279/admin.html` để tạo nhanh dữ liệu test. Lời khuyên: Hãy tạo một tài khoản có Username là `admin` để trải nghiệm toàn quyền.

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
| GET | `/api/Projects` | Lấy danh sách project (Admin lấy All) | Khóa |
| POST | `/api/Projects` | Tạo project mới | Khóa |
| GET | `/api/Tasks` | Lấy toàn bộ task | Khóa |
| GET | `/api/Tasks/project/{id}`| Lấy task của 1 project | Khóa (Bảo vệ Owner) |
| POST | `/api/Tasks` | Tạo task mới | Khóa (Bảo vệ Owner) |
| PATCH | `/api/Tasks/{id}/column`| Cập nhật cột (Drag-Drop) | Khóa (Bảo vệ Owner) |

## 🗺 Roadmap / TODO (Đã hoàn thành!)

- [x] Mã hoá mật khẩu (BCrypt)
- [x] Xác thực & phân quyền (JWT Authentication)
- [x] Thắt chặt CORS Policy cho production
- [x] Nâng cấp Frontend Desktop sang mô hình MVVM toàn diện (Service Layer, ICommand)
- [x] Triển khai Tiêm phụ thuộc (Dependency Injection) trên Desktop
- [x] Loại bỏ Event Code-Behind bằng thư viện Gong-Wpf-DragDrop
- [x] Cài đặt Role-Based Access Control (RBAC) (Admin vs Nhân viên)
- [x] Công cụ Web Dashboard Data Management
- [ ] Trang Team Directory / Files trên Desktop (Sắp tới)

## 🤝 Đóng góp

Pull request và issue đều được hoan nghênh.
