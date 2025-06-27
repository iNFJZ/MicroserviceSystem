# Microservice System

Hệ thống microservice được xây dựng bằng .NET 8 với các service chính:
- **AuthService**: Quản lý xác thực và phân quyền với JWT, Redis cache, và session management
- **FileService**: Quản lý file upload/download với MinIO object storage và RabbitMQ event publishing
- **GatewayApi**: API Gateway sử dụng Ocelot để route requests
- **WorkerService**: Background service xử lý file events từ RabbitMQ

## 🏗️ Kiến trúc hệ thống

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Client    │    │   Client    │    │   Client    │
└─────┬───────┘    └─────┬───────┘    └─────┬───────┘
      │                  │                  │
      └──────────────────┼──────────────────┘
                         │
              ┌──────────▼──────────┐
              │   GatewayApi        │
              │   (Ocelot)          │
              │   Port: 5050        │
              └──────────┬──────────┘
                         │
        ┌────────────────┼────────────────┐
        │                │                │
┌───────▼──────┐  ┌──────▼──────┐  ┌─────▼─────┐
│ AuthService  │  │ FileService │  │ Worker    │
│ Port: 5001   │  │ Port: 5002  │  │ Service   │
└──────┬───────┘  └──────┬──────┘  └─────┬─────┘
       │                 │               │
       │                 │               │
┌──────▼──────┐  ┌──────▼──────┐  ┌─────▼─────┐
│ PostgreSQL  │  │    MinIO    │  │ RabbitMQ  │
│ Port: 5432  │  │ Port: 9000  │  │ Port: 5672│
└─────────────┘  └─────────────┘  └───────────┘
       │                 │               │
       │                 │               │
┌──────▼──────┐  ┌──────▼──────┐  ┌─────▼─────┐
│    Redis    │  │   MinIO     │  │ RabbitMQ  │
│ Port: 6379  │  │  Console    │  │ Management│
└─────────────┘  │ Port: 9001  │  │ Port: 15672│
                 └─────────────┘  └───────────┘
```

## 🚀 Tính năng chính

### AuthService
- **Đăng ký/Đăng nhập**: JWT token authentication
- **Session Management**: Quản lý phiên đăng nhập với Redis
- **Token Validation**: Xác thực JWT token
- **User Sessions**: Xem và quản lý phiên đăng nhập
- **Logout**: Đăng xuất và vô hiệu hóa token

### FileService
- **File Upload**: Upload file lên MinIO (giới hạn 10MB/file)
- **File Download**: Tải file từ MinIO
- **File List**: Liệt kê tất cả file
- **File Delete**: Xóa file
- **Event Publishing**: Publish events đến RabbitMQ khi upload/download/delete

### GatewayApi
- **API Routing**: Route requests đến các microservice
- **Load Balancing**: Cân bằng tải giữa các service
- **Request Aggregation**: Tập trung các API endpoints

### WorkerService
- **Event Processing**: Xử lý file events từ RabbitMQ
- **Background Jobs**: Chạy các tác vụ nền
- **Retry Logic**: Xử lý retry cho failed operations

## 🛠️ Công nghệ sử dụng

- **.NET 8**: Framework chính
- **PostgreSQL**: Database chính
- **Redis**: Cache và session storage
- **MinIO**: Object storage cho files
- **RabbitMQ**: Message queue
- **Ocelot**: API Gateway
- **JWT**: Authentication
- **Docker**: Containerization
- **Entity Framework**: ORM

## 🚀 Quick Start

### Prerequisites
- Docker & Docker Compose
- .NET 8 SDK (cho development)

### 1. Clone repository
```bash
git clone <repository-url>
cd MicroserviceSystem
```

### 2. Chạy toàn bộ hệ thống
```bash
# Build và chạy tất cả services
docker-compose up --build

# Chạy ở background
docker-compose up -d
```

### 3. Chạy từng service riêng lẻ
```bash
# Chỉ chạy infrastructure services
docker-compose up postgres redis rabbitmq minio

# Chạy AuthService
docker-compose up auth-service

# Chạy FileService
docker-compose up file-service

# Chạy GatewayApi
docker-compose up gateway-api

# Chạy WorkerService
docker-compose up worker-service
```

### 4. Dừng hệ thống
```bash
# Dừng tất cả services
docker-compose down

# Dừng và xóa volumes
docker-compose down -v
```

## 📊 Ports và Endpoints

| Service | Port | Endpoint | Mô tả |
|---------|------|----------|-------|
| GatewayApi | 5050 | http://localhost:5050 | API Gateway |
| AuthService | 5001 | http://localhost:5001 | Dịch vụ xác thực |
| FileService | 5002 | http://localhost:5002 | Dịch vụ file |
| PostgreSQL | 5432 | localhost:5432 | Database |
| Redis | 6379 | localhost:6379 | Cache |
| RabbitMQ | 5672 | localhost:5672 | Message Queue |
| RabbitMQ Management | 15672 | http://localhost:15672 | RabbitMQ UI |
| MinIO | 9000 | localhost:9000 | Object Storage |
| MinIO Console | 9001 | http://localhost:9001 | MinIO UI |

## 🔧 Cấu hình môi trường

### Database (PostgreSQL)
- **Host**: auth-postgres
- **Port**: 5432
- **Database**: auth_db
- **Username**: postgres
- **Password**: 123456

### Redis
- **Connection String**: redis:6379

### MinIO
- **Endpoint**: minio:9000
- **Access Key**: minio
- **Secret Key**: minio123
- **Bucket**: mybucket
- **Use SSL**: false

### RabbitMQ
- **Username**: guest
- **Password**: guest

## 📡 API Endpoints

### GatewayApi (Port 5050)
- `/api/auth/*` - Chuyển tiếp đến AuthService
- `/api/file/*` - Chuyển tiếp đến FileService

### AuthService (Port 5001)
- `POST /api/auth/register` - Đăng ký người dùng
- `POST /api/auth/login` - Đăng nhập
- `POST /api/auth/logout` - Đăng xuất
- `POST /api/auth/validate` - Xác thực token
- `GET /api/auth/sessions` - Lấy danh sách phiên đăng nhập
- `DELETE /api/auth/sessions/{sessionId}` - Xóa phiên đăng nhập

### FileService (Port 5002)
- `POST /api/file/upload` - Upload file (multipart/form-data)
- `GET /api/file/download/{fileName}` - Download file
- `DELETE /api/file/delete/{fileName}` - Xóa file
- `GET /api/file/list` - Liệt kê tất cả file

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test AuthService.Tests
dotnet test WorkerService.Tests
```

## 📊 Monitoring

### Health Checks
- Gateway API: `http://localhost:5050/health`
- Auth Service: `http://localhost:5001/health`
- File Service: `http://localhost:5002/health`

### Logs
```bash
# View logs for specific service
docker logs auth-service
docker logs file-service
docker logs gateway-api
docker logs worker-service
```

### Management UIs
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)
- **MinIO Console**: http://localhost:9001 (minio/minio123)

## 🔒 Security

- **JWT Authentication**: Token-based authentication
- **Session Management**: Redis-based session storage
- **File Validation**: File size và type validation
- **Input Validation**: Model validation với Data Annotations

```
MicroserviceSystem/
├── AuthService/              # Dịch vụ xác thực
│   ├── Controllers/          # API Controllers
│   ├── Services/             # Business logic
│   ├── Models/               # Entity models
│   ├── DTOs/                 # Data Transfer Objects
│   ├── Repositories/         # Data access layer
│   └── Middleware/           # Custom middleware
├── FileService/              # Dịch vụ quản lý file
│   ├── Controllers/          # API Controllers
│   ├── Services/             # Business logic
│   ├── Models/               # Event models
│   └── DTOs/                 # Data Transfer Objects
├── GatewayApi/               # API Gateway
│   └── ocelot.json          # Ocelot configuration
├── WorkerService/            # Background service
│   ├── Services/             # Business logic
│   ├── Models/               # Event models
│   └── Configuration/        # Configuration classes
├── AuthService.Tests/        # Unit tests cho AuthService
├── WorkerService.Tests/      # Unit tests cho WorkerService
├── docker-compose.yml        # Docker Compose configuration
├── Dockerfile               # Multi-stage Docker build
└── README.md                # Tài liệu này
```