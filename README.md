# Microservice System

Hệ thống microservices bao gồm:
- **AuthService**: Dịch vụ xác thực và quản lý người dùng
- **FileService**: Dịch vụ quản lý file với MinIO
- **GatewayApi**: API Gateway sử dụng Ocelot

## Cấu trúc dự án

```
MicroserviceSystem/
├── AuthService/          # Dịch vụ xác thực
├── FileService/          # Dịch vụ quản lý file
├── GatewayApi/           # API Gateway
├── AuthService.Tests/    # Unit tests
├── docker-compose.yml    # Cấu hình Docker Compose
├── Dockerfile           # Multi-stage Docker build
└── README.md            # Tài liệu này
```

## Yêu cầu hệ thống

- Docker Desktop
- Docker Compose
- .NET 8.0 SDK (để phát triển)

## Cách chạy dự án

### 1. Chạy toàn bộ hệ thống với Docker Compose

```bash
# Build và chạy tất cả services
docker-compose up --build

# Chạy ở background
docker-compose up -d --build
```

### 2. Chạy từng service riêng lẻ

```bash
# Chỉ chạy database và Redis
docker-compose up postgres redis

# Chạy AuthService
docker-compose up auth-service

# Chạy FileService
docker-compose up file-service

# Chạy GatewayApi
docker-compose up gateway-api
```

### 3. Dừng hệ thống

```bash
# Dừng tất cả services
docker-compose down

# Dừng và xóa volumes
docker-compose down -v
```

## Ports và Endpoints

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

## Cấu hình môi trường

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

## API Endpoints

### GatewayApi (Port 5050)
- `/api/auth/*` - Chuyển tiếp đến AuthService
- `/api/file/*` - Chuyển tiếp đến FileService

### AuthService (Port 5001)
- `POST /api/auth/register` - Đăng ký người dùng
- `POST /api/auth/login` - Đăng nhập
- `POST /api/auth/logout` - Đăng xuất

### FileService (Port 5002)
- `POST /api/file/upload` - Upload file
- `GET /api/file/download/{filename}` - Download file
- `DELETE /api/file/{filename}` - Xóa file

## Cấu trúc Docker

Dockerfile sử dụng multi-stage build:
1. **base**: Runtime image (.NET 8.0 ASP.NET)
2. **build**: Build image (.NET 8.0 SDK)
3. **publish**: Publish stage
4. **final**: Final runtime image

Mỗi service được build từ cùng một Dockerfile nhưng chạy với command khác nhau:
- AuthService: `dotnet /app/AuthService/AuthService.dll`
- FileService: `dotnet /app/FileService/FileService.dll`
- GatewayApi: `dotnet /app/GatewayApi/GatewayApi.dll` 
