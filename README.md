# Microservice System

Hệ thống microservice .NET 8 gồm các service:
- **AuthService**: Xác thực, phân quyền, quản lý user, JWT, Redis session.
- **FileService**: Upload/download/list/delete file với MinIO, gửi event qua RabbitMQ.
- **GatewayApi**: API Gateway dùng Ocelot.
- **EmailService**: Nhận event từ RabbitMQ, gửi email notification qua Gmail SMTP.

## 🏗️ Kiến trúc tổng thể

```
Client <-> GatewayApi (Ocelot)
                |
    ┌────────────┼────────────┐
    |            |            |
AuthService  FileService  EmailService
    |            |            |
PostgreSQL    MinIO      RabbitMQ
    |            |            |
   Redis      MinIO UI   RabbitMQ UI
```

## 🚀 Tính năng nổi bật

### AuthService
- Đăng ký, đăng nhập, xác thực JWT, quản lý session với Redis.
- Đăng ký thành công sẽ gửi event qua RabbitMQ để EmailService gửi mail chào mừng.

### FileService
- Upload, download, list, delete file với MinIO.
- Mỗi thao tác upload/download/delete sẽ gửi event qua RabbitMQ để EmailService gửi mail thông báo.
- Tất cả endpoint đều yêu cầu JWT hợp lệ.

### EmailService
- Consume event từ RabbitMQ.
- Gửi email notification qua Gmail SMTP (hỗ trợ app password).

### GatewayApi
- Route request đến các service qua Ocelot.
- Hỗ trợ load balancing, request aggregation.

## 🛠️ Công nghệ sử dụng

- .NET 8, Entity Framework Core
- PostgreSQL, Redis, MinIO, RabbitMQ
- Ocelot, JWT, Docker Compose
- Gmail SMTP (app password) cho EmailService

## 🚀 Quick Start

### Prerequisites
- Docker & Docker Compose
- .NET 8 SDK

### 1. Clone repository
```bash
git clone <repository-url>
cd MicroserviceSystem
```

### 2. Build & chạy toàn bộ hệ thống
```bash
docker compose up --build
# hoặc chạy nền
docker compose up -d
```

### 3. Dừng hệ thống
```bash
docker compose down
# Xóa luôn volume (xóa sạch data)
docker compose down -v
```

## 📡 API Endpoints

### AuthService (http://localhost:5001)
- `POST /api/auth/register` - Đăng ký user
- `POST /api/auth/login` - Đăng nhập, nhận JWT
- `POST /api/auth/logout` - Đăng xuất
- `POST /api/auth/validate` - Kiểm tra token hợp lệ
- `GET /api/auth/sessions` - Danh sách phiên đăng nhập
- `DELETE /api/auth/sessions/{sessionId}` - Xóa phiên
- `POST /api/auth/forgot-password` - Quên mật khẩu (gửi email reset)
- `POST /api/auth/reset-password` - Đặt lại mật khẩu (dùng token)
- `POST /api/auth/change-password` - Đổi mật khẩu (yêu cầu đăng nhập)

### FileService (http://localhost:5002)
- `POST /api/file/upload` - Upload file (multipart/form-data)
- `GET /api/file/download/{fileName}` - Download file
- `DELETE /api/file/delete/{fileName}` - Xóa file
- `GET /api/file/list` - Liệt kê file

### GatewayApi (http://localhost:5050)
- `/api/auth/*` - Proxy đến AuthService
- `/api/file/*` - Proxy đến FileService

## 📨 Email Notification (Event-driven)
- Đăng ký, upload, download, delete file đều gửi event qua RabbitMQ.
- EmailService consume event, gửi email với nội dung động.
- **Reset Password**: Gửi email chứa token reset (có hạn 15 phút).

## 🗄️ Cấu hình môi trường

- **PostgreSQL**: auth_db, user: postgres, pass: 123456, port: 5432
- **Redis**: redis:6379
- **MinIO**: minio:9000, access: minio, secret: minio123, bucket: mybucket
- **RabbitMQ**: guest/guest, port: 5672, UI: 15672
- **Gmail SMTP**: cấu hình trong EmailService/appsettings.json

## 🧪 Testing

```bash
dotnet test
```

## 📝 Lưu ý thực tế

- **Migration DB**: Nếu gặp lỗi "relation ... does not exist", hãy chạy migration cho AuthService.
- **Reset Password**: Token có hạn 15 phút, sau khi reset sẽ vô hiệu hóa tất cả session.
- **Change Password**: Yêu cầu mật khẩu hiện tại, sau khi đổi sẽ vô hiệu hóa tất cả session.

## 📊 Monitoring & UI

- **RabbitMQ UI**: http://localhost:15672 (guest/guest)
- **MinIO UI**: http://localhost:9001 (minio/minio123)
- **Swagger**: http://localhost:5001/swagger, http://localhost:5002/swagger

## 📂 Cấu trúc thư mục

```
MicroserviceSystem/
├── AuthService/
├── FileService/
├── GatewayApi/
├── EmailService/
├── AuthService.Tests/
├── docker-compose.yml
├── Dockerfile
└── README.md
```
