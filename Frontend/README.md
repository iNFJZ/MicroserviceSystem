# Frontend Admin System

## Cấu trúc dự án

```
Frontend/
├── html/
│   ├── auth/           # Trang authentication (login, register, etc.)
│   └── admin/          # Trang admin (dashboard, user management, etc.)
├── js/
│   ├── admin-auth.js   # Authentication handler chung cho tất cả trang admin
│   ├── user-list-backend.js
│   ├── all-users-backend.js
│   ├── deactive-users-backend.js
│   └── ...
└── nginx.conf          # Cấu hình nginx với routing linh hoạt
```

## Cách thêm trang mới

### 1. Tạo file HTML mới

Tạo file HTML mới trong thư mục `html/admin/`:

```html
<!DOCTYPE html>
<html lang="en" class="light-style layout-navbar-fixed layout-menu-fixed" dir="ltr" data-theme="theme-default" data-assets-path="../../assets/" data-template="vertical-menu-template">
<head>
    <!-- Include các file CSS cần thiết -->
    <title>New Page - Admin</title>
    <!-- ... other head content ... -->
</head>
<body>
    <!-- Layout wrapper -->
    <div class="layout-wrapper layout-content-navbar">
        <div class="layout-container">
            <!-- Menu -->
            <aside id="layout-menu" class="layout-menu menu-vertical menu bg-menu-theme">
                <!-- Copy menu từ trang khác và thêm link mới -->
                <ul class="menu-inner py-1">
                    <li class="menu-item">
                        <a href="new-page.html" class="menu-link">
                            <i class="menu-icon tf-icons ti ti-new"></i>
                            <div data-i18n="New Page">New Page</div>
                        </a>
                    </li>
                </ul>
            </aside>
            
            <!-- Content -->
            <div class="layout-page">
                <!-- Navbar -->
                <nav class="layout-navbar container-xxl navbar navbar-expand-xl navbar-detached align-items-center bg-navbar-theme" id="layout-navbar">
                    <!-- ... navbar content ... -->
                </nav>
                
                <!-- Content wrapper -->
                <div class="content-wrapper">
                    <div class="container-xxl flex-grow-1 container-p-y">
                        <h4 class="py-3 mb-4">
                            <span class="text-muted fw-light">Admin /</span> New Page
                        </h4>
                        
                        <!-- Your page content here -->
                        <div class="card">
                            <div class="card-header">
                                <h5 class="card-title">New Page Content</h5>
                            </div>
                            <div class="card-body">
                                <!-- Your content -->
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
    
    <!-- Core JS -->
    <script src="../../assets/vendor/libs/jquery/jquery.js"></script>
    <!-- ... other vendor scripts ... -->
    
    <!-- Page JS -->
    <script src="../../js/admin-auth.js"></script>
    <script src="../../js/new-page-backend.js"></script>
</body>
</html>
```

### 2. Tạo file JavaScript backend

Tạo file JavaScript mới trong thư mục `js/`:

```javascript
// new-page-backend.js
$(function () {
    // Your page-specific JavaScript code here
    
    // Example: Load data from API
    loadPageData();
});

async function loadPageData() {
    try {
        const token = window.adminAuth.getAuthToken();
        const response = await fetch('http://localhost:5050/api/your-endpoint', {
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });
        
        if (response.ok) {
            const data = await response.json();
            // Handle your data
        }
    } catch (error) {
        console.error('Error loading data:', error);
    }
}

// Your other functions...
```

### 3. Truy cập trang mới

Sau khi tạo file, bạn có thể truy cập trang mới bằng các URL sau:

- `http://localhost:8080/admin/new-page.html`
- `http://localhost:8080/new-page.html`

**Không cần cấu hình nginx thêm!**

## Cấu hình nginx linh hoạt

Nginx đã được cấu hình với các pattern linh hoạt:

```nginx
# Flexible admin page routing - matches any admin page
location ~ ^/admin/([^/]+)\.html$ {
    try_files /html/admin/$1.html /html/index.html;
    add_header 'Access-Control-Allow-Origin' '*' always;
}

# Direct page access without /admin/ prefix
location ~ ^/(user-list|all-users|deactive-users|index)\.html$ {
    try_files /html/admin/$1.html /html/index.html;
    add_header 'Access-Control-Allow-Origin' '*' always;
}
```

Điều này có nghĩa là:
- Bất kỳ file HTML nào trong `html/admin/` sẽ tự động có thể truy cập qua `/admin/filename.html`
- Không cần thêm route mới cho mỗi trang

## Authentication

Tất cả trang admin sẽ tự động có authentication thông qua file `admin-auth.js`:

- Kiểm tra token khi load trang
- Redirect về login nếu không có token
- Xử lý logout
- Validate token với server

## Best Practices

1. **Luôn include admin-auth.js** trong tất cả trang admin
2. **Sử dụng window.adminAuth.getAuthToken()** để lấy token
3. **Sử dụng window.adminAuth.getAuthHeaders()** để lấy headers cho API calls
4. **Copy menu structure** từ trang khác để đảm bảo consistency
5. **Sử dụng cùng layout template** để đảm bảo UI consistency

## Ví dụ thêm trang mới

### Cách 1: Sử dụng script tự động (Khuyến nghị)

```bash
# Tạo trang settings
node create-page.js settings "System Settings" ti-settings "Settings"

# Tạo trang reports
node create-page.js reports "User Reports" ti-chart-bar "Reports"

# Tạo trang analytics
node create-page.js analytics "Analytics Dashboard" ti-chart-pie "Analytics"
```

Script sẽ tự động:
- Tạo file HTML từ template
- Tạo file JavaScript với cấu trúc cơ bản
- Thay thế các placeholder với thông tin bạn cung cấp

### Cách 2: Tạo thủ công

#### Bước 1: Copy template
```bash
cp html/admin/template.html html/admin/your-page.html
```

#### Bước 2: Tạo file JavaScript
```bash
# Tạo file js/your-page-backend.js
```

#### Bước 3: Truy cập
```
http://localhost:8080/admin/your-page.html
```

**Không cần thay đổi nginx.conf!** 