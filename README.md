# Issue Tracking API

A RESTful Issue Tracking API built with ASP.NET Core 8.

## Tech Stack

- ASP.NET Core 8
- Entity Framework Core (SQL Server)
- JWT Authentication (BCrypt password hashing)
- Swagger / OpenAPI

## Tính năng

- **Auth**: đăng ký, đăng nhập, trả về JWT. User đầu tiên đăng ký sẽ tự động có role `Admin` (toàn quyền hệ thống), các user sau là `Member`.
- **Project**: CRUD project, quản lý thành viên theo từng project với vai trò `Owner` / `Member`.
- **Issue**: CRUD issue trong project, gồm Status (`Open`, `InProgress`, `InReview`, `Closed`), Priority (`Low`, `Medium`, `High`, `Critical`), gán Assignee, gắn Label. Có filter theo status/priority/assignee.
- **Comment**: bình luận trên issue.
- **Label**: gắn nhãn màu cho issue theo từng project.

### Quy tắc phân quyền cơ bản

- Chỉ thành viên của project mới xem/tạo issue, comment, label trong project đó.
- Sửa/xóa issue: người tạo (reporter), người được gán (assignee), Owner của project, hoặc Admin hệ thống.
- Sửa/xóa comment: tác giả comment, Owner của project, hoặc Admin hệ thống.
- Quản lý thành viên project (thêm/xóa/đổi vai trò), xóa project, xóa label: chỉ Owner của project hoặc Admin hệ thống.

## Chạy project lần đầu

1. Mở solution trong Visual Studio (hoặc dùng CLI), vào thư mục project:
   ```
   cd IssueTrackingAPI/IssueTrackingAPI
   ```
2. Cài package (nếu dùng CLI):
   ```
   dotnet restore
   ```
3. **Cấu hình secret bằng User Secrets** (KHÔNG điền secret thật vào `appsettings.json` — file này commit lên git):
   ```
   dotnet user-secrets init
   dotnet user-secrets set "Jwt:Key" "<chuỗi random >= 32 ký tự>"
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection string SQL Server của bạn>"
   ```
   Khi deploy thật (server/cloud), thay User Secrets bằng biến môi trường hoặc secret manager của platform.
4. Update database theo migration đã có sẵn trong repo:
   ```
   dotnet ef database update
   ```
   (nếu chưa có `dotnet-ef`: `dotnet tool install --global dotnet-ef`)
5. Chạy project:
   ```
   dotnet run
   ```
6. Mở Swagger UI (`/swagger`) để test API. Gọi `POST /api/auth/register` để tạo user đầu tiên (tự động là Admin), copy `token` trả về và bấm **Authorize** trên Swagger, nhập `Bearer {token}`.

## Cấu trúc thư mục

```
Controllers/   Auth, Projects, Issues, Comments, Labels
Services/      Business logic + kiểm tra quyền
Models/        Entity (User, Project, ProjectMember, Issue, Comment, Label, IssueLabel) + Enums
DTOs/          Request/response models theo module
Data/          AppDbContext (EF Core)
Common/        Exception xử lý lỗi tập trung, extension đọc claims từ JWT
Settings/      JwtSettings
```
