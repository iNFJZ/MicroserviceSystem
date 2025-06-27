# Microservice System

Hệ thống microservice được xây dựng bằng .NET 8 với các service chính:
- **AuthService**: Quản lý xác thực và phân quyền
- **FileService**: Quản lý file upload/download
- **GatewayApi**: API Gateway sử dụng Ocelot
- **WorkerService**: Background service xử lý file

## 🚀 CI/CD Pipeline

Dự án này sử dụng GitHub Actions để tự động hóa quá trình build, test và deploy.

### Workflows

#### 1. CI Pipeline (`ci-pipeline.yml`)
- **Trigger**: Push/Pull Request vào `main` hoặc `develop`
- **Chức năng**:
  - Build và test toàn bộ solution
  - Chạy unit tests với code coverage
  - Security scanning với Trivy
  - Build và push Docker images lên GitHub Container Registry
  - Quét lỗ hổng bảo mật cho Docker images

#### 2. CD Pipeline - Staging (`cd-staging.yml`)
- **Trigger**: CI Pipeline thành công trên branch `develop`
- **Chức năng**:
  - Deploy tự động lên staging environment
  - Health check sau deployment
  - Sử dụng port khác để tránh xung đột với development

#### 3. CD Pipeline - Production (`cd-production.yml`)
- **Trigger**: CI Pipeline thành công trên branch `main` hoặc manual trigger
- **Chức năng**:
  - Deploy lên production với manual approval
  - Health check chi tiết
  - Tạo deployment tags
  - Restart policy cho production

#### 4. Security Scan (`security-scan.yml`)
- **Trigger**: Daily schedule, manual, hoặc push
- **Chức năng**:
  - OWASP Dependency Check
  - CodeQL Analysis
  - Container vulnerability scanning
  - Secret scanning với TruffleHog

#### 5. Rollback (`rollback.yml`)
- **Trigger**: Manual trigger
- **Chức năng**:
  - Rollback về version trước đó
  - Hỗ trợ cả staging và production
  - Health check sau rollback

### Environment Variables

Cần thiết lập các secrets sau trong GitHub repository:

#### Staging Environment
- `STAGING_DB_PASSWORD`: Mật khẩu database staging

#### Production Environment
- `PRODUCTION_DB_PASSWORD`: Mật khẩu database production

### Port Mapping

| Environment | Gateway | Auth Service | File Service | Database | Redis | RabbitMQ | MinIO |
|-------------|---------|--------------|--------------|----------|-------|----------|-------|
| Development | 5050    | 5001         | 5002         | 5432     | 6379  | 5672     | 9000  |
| Staging     | 5051    | 5004         | 5005         | 5433     | 6380  | 5673     | 9002  |
| Production  | 5052    | 5006         | 5007         | 5434     | 6381  | 5674     | 9004  |

## 🛠️ Development

### Prerequisites
- .NET 8 SDK
- Docker & Docker Compose
- PostgreSQL 14
- Redis
- RabbitMQ
- MinIO

### Quick Start

1. **Clone repository**
```bash
git clone <repository-url>
cd MicroserviceSystem
```

2. **Start infrastructure services**
```bash
docker-compose up -d postgres redis rabbitmq minio
```

3. **Run migrations**
```bash
cd AuthService
dotnet ef database update
```

4. **Start services**
```bash
# Development
docker-compose up

# Hoặc chạy từng service riêng lẻ
dotnet run --project AuthService
dotnet run --project FileService
dotnet run --project GatewayApi
dotnet run --project WorkerService
```

### Testing

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

## 🔒 Security

Pipeline bao gồm các bước bảo mật:
- **Dependency scanning**: Kiểm tra lỗ hổng trong dependencies
- **Code analysis**: Phân tích mã nguồn với CodeQL
- **Container scanning**: Quét lỗ hổng trong Docker images
- **Secret scanning**: Tìm kiếm secrets bị lộ trong code

## 📈 Deployment Strategy

1. **Development**: Push code → CI Pipeline → Build & Test
2. **Staging**: Merge vào `develop` → Auto deploy staging
3. **Production**: Merge vào `main` → Manual approval → Deploy production

### Rollback Process
1. Vào GitHub Actions → Rollback workflow
2. Chọn environment (staging/production)
3. Chọn version để rollback (hoặc để trống để rollback về version trước)
4. Confirm deployment

## 🤝 Contributing

1. Fork repository
2. Tạo feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Tạo Pull Request

## 📝 License

This project is licensed under the MIT License.

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
- `POST /api/file/upload`