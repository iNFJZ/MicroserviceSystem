class UserManagement {
    constructor() {
        this.currentPage = 1;
        this.pageSize = 10;
        this.totalPages = 0;
        this.totalCount = 0;
        this.currentFilters = {};
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.loadUsers();
        this.loadStatistics();
    }

    setupEventListeners() {
        const searchInput = document.querySelector('.datatables-search');
        if (searchInput) {
            searchInput.addEventListener('input', this.debounce(() => {
                this.currentFilters.search = searchInput.value;
                this.currentPage = 1;
                this.loadUsers();
            }, 500));
        }

        const statusFilter = document.querySelector('.user_status');
        if (statusFilter) {
            statusFilter.addEventListener('change', () => {
                this.currentFilters.status = statusFilter.value;
                this.currentPage = 1;
                this.loadUsers();
            });
        }

        const roleFilter = document.querySelector('.user_role');
        if (roleFilter) {
            roleFilter.addEventListener('change', () => {
                this.currentFilters.role = roleFilter.value;
                this.currentPage = 1;
                this.loadUsers();
            });
        }

        document.addEventListener('click', (e) => {
            if (e.target.classList.contains('page-link')) {
                e.preventDefault();
                const page = parseInt(e.target.dataset.page);
                if (page && page !== this.currentPage) {
                    this.currentPage = page;
                    this.loadUsers();
                }
            }
        });

        const addUserBtn = document.querySelector('.add-new-user');
        if (addUserBtn) {
            addUserBtn.addEventListener('click', () => {
                this.showAddUserModal();
            });
        }

        const addUserForm = document.getElementById('addNewUserForm');
        if (addUserForm) {
            addUserForm.addEventListener('submit', (e) => {
                e.preventDefault();
                this.handleAddUser();
            });
        }
    }

    async loadUsers() {
        try {
            const params = new URLSearchParams({
                page: this.currentPage,
                pageSize: this.pageSize,
                search: this.currentFilters.search || '',
                status: this.currentFilters.status || '',
                role: this.currentFilters.role || '',
                sortBy: this.currentFilters.sortBy || '',
                sortOrder: this.currentFilters.sortOrder || 'asc'
            });
            const response = await fetch(`http://localhost:5050/api/user?${params}`, {
                headers: {
                    'Authorization': `Bearer ${this.getToken()}`,
                    'Content-Type': 'application/json'
                }
            });
            if (!response.ok) {
                throw new Error('Failed to load users');
            }
            const data = await response.json();
            if (data.success) {
                this.renderUsers(data.data);
                this.renderPagination(data.pagination);
                this.totalPages = data.pagination.totalPages;
                this.totalCount = data.pagination.totalCount;
            } else {
                this.showError('Failed to load users: ' + data.message);
            }
        } catch (error) {
            console.error('Error loading users:', error);
            this.showError('Failed to load users');
        }
    }

    async loadStatistics() {
        try {
            const response = await fetch('http://localhost:5050/api/user/statistics', {
                headers: {
                    'Authorization': `Bearer ${this.getToken()}`,
                    'Content-Type': 'application/json'
                }
            });
            if (!response.ok) {
                throw new Error('Failed to load statistics');
            }
            const data = await response.json();
            if (data.success) {
                this.updateStatistics(data.data);
            }
        } catch (error) {
            console.error('Error loading statistics:', error);
        }
    }

    renderUsers(users) {
        const table = document.querySelector('.datatables-users');
        if (!table) return;
        let tbody = table.querySelector('tbody');
        if (!tbody) {
            tbody = document.createElement('tbody');
            table.appendChild(tbody);
        }
        tbody.innerHTML = '';
        users.forEach(user => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>
                    <div class="d-flex justify-content-start align-items-center">
                        <div class="avatar-wrapper">
                            <div class="avatar me-3">
                                <img src="${user.profilePicture || '../../assets/img/avatars/1.png'}" alt="Avatar" class="rounded-circle" />
                            </div>
                        </div>
                        <div class="d-flex flex-column">
                            <span class="fw-medium">${user.fullName || ''}</span>
                            <small class="text-muted">${user.username}</small>
                        </div>
                    </div>
                </td>
                <td>${user.email}</td>
                <td>${user.phoneNumber || ''}</td>
                <td>${user.dateOfBirth ? this.formatDate(user.dateOfBirth) : ''}</td>
                <td>${user.address || ''}</td>
                <td>${user.bio || ''}</td>
                <td><span class="badge bg-label-${this.getStatusBadgeClass(user.status)}">${this.getStatusText(user.status)}</span></td>
                <td>${user.loginProvider || ''}</td>
                <td><span class="badge bg-label-${user.isVerified ? 'success' : 'warning'}">${user.isVerified ? 'Verified' : 'Unverified'}</span></td>
                <td>${this.formatDate(user.createdAt)}</td>
                <td>${user.lastLoginAt ? this.formatDate(user.lastLoginAt) : ''}</td>
                <td>${user.updatedAt ? this.formatDate(user.updatedAt) : ''}</td>
                <td>
                    <div class="d-inline-block text-nowrap">
                        <button class="btn btn-sm btn-icon btn-text-secondary rounded-pill btn-icon dropdown-toggle hide-arrow" data-bs-toggle="dropdown">
                            <i class="ti ti-dots-vertical"></i>
                        </button>
                        <div class="dropdown">
                            <ul class="dropdown-menu">
                                <li><a class="dropdown-item" href="javascript:void(0);" onclick="userManagement.editUser('${user.id}')"><i class="ti ti-edit me-1"></i>Edit</a></li>
                                <li><a class="dropdown-item" href="javascript:void(0);" onclick="userManagement.viewUser && userManagement.viewUser('${user.id}')"><i class="ti ti-eye me-1"></i>View</a></li>
                                <li><hr class="dropdown-divider" /></li>
                                <li><a class="dropdown-item text-danger delete-record" href="javascript:void(0);" onclick="userManagement.deleteUser('${user.id}')"><i class="ti ti-trash me-1"></i>Delete</a></li>
                            </ul>
                        </div>
                    </div>
                </td>
            `;
            tbody.appendChild(row);
        });
    }

    renderPagination(pagination) {
        const paginationContainer = document.querySelector('.datatables-pagination');
        if (!paginationContainer) return;
        let paginationHTML = '<ul class="pagination">';
        paginationHTML += `
            <li class="page-item ${pagination.hasPreviousPage ? '' : 'disabled'}">
                <a class="page-link" href="javascript:void(0);" data-page="${pagination.page - 1}">
                    <i class="ti ti-chevron-left"></i>
                </a>
            </li>
        `;
        const startPage = Math.max(1, pagination.page - 2);
        const endPage = Math.min(pagination.totalPages, pagination.page + 2);
        for (let i = startPage; i <= endPage; i++) {
            paginationHTML += `
                <li class="page-item ${i === pagination.page ? 'active' : ''}">
                    <a class="page-link" href="javascript:void(0);" data-page="${i}">${i}</a>
                </li>
            `;
        }
        paginationHTML += `
            <li class="page-item ${pagination.hasNextPage ? '' : 'disabled'}">
                <a class="page-link" href="javascript:void(0);" data-page="${pagination.page + 1}">
                    <i class="ti ti-chevron-right"></i>
                </a>
            </li>
        `;
        paginationHTML += '</ul>';
        paginationContainer.innerHTML = paginationHTML;
    }

    updateStatistics(stats) {
        const totalUsersCard = document.querySelector('.card:nth-child(1) .content-left h3');
        if (totalUsersCard) totalUsersCard.textContent = stats.totalUsers?.toLocaleString() || '0';
        const activeUsersCard = document.querySelector('.card:nth-child(3) .content-left h3');
        if (activeUsersCard) activeUsersCard.textContent = stats.activeUsers?.toLocaleString() || '0';
        const verifiedUsersCard = document.querySelector('.card:nth-child(2) .content-left h3');
        if (verifiedUsersCard) verifiedUsersCard.textContent = stats.verifiedUsers?.toLocaleString() || '0';
        const pendingUsersCard = document.querySelector('.card:nth-child(4) .content-left h3');
        if (pendingUsersCard) pendingUsersCard.textContent = stats.unverifiedUsers?.toLocaleString() || '0';
    }

    async editUser(userId) {
        try {
            const response = await fetch(`http://localhost:5050/api/user/${userId}`, {
                headers: {
                    'Authorization': `Bearer ${this.getToken()}`,
                    'Content-Type': 'application/json'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to load user');
            }

            const data = await response.json();
            
            if (data.success) {
                this.showEditUserModal(data.data);
            } else {
                this.showError('Failed to load user: ' + data.message);
            }
        } catch (error) {
            console.error('Error loading user:', error);
            this.showError('Failed to load user');
        }
    }

    async deleteUser(userId) {
        if (!confirm('Are you sure you want to delete this user?')) {
            return;
        }

        try {
            const response = await fetch(`http://localhost:5050/api/user/${userId}`, {
                method: 'DELETE',
                headers: {
                    'Authorization': `Bearer ${this.getToken()}`,
                    'Content-Type': 'application/json'
                }
            });

            if (!response.ok) {
                throw new Error('Failed to delete user');
            }

            const data = await response.json();
            
            if (data.success) {
                this.showSuccess('User deleted successfully');
                this.loadUsers();
                this.loadStatistics();
            } else {
                this.showError('Failed to delete user: ' + data.message);
            }
        } catch (error) {
            console.error('Error deleting user:', error);
            this.showError('Failed to delete user');
        }
    }

    showAddUserModal() {
        const modal = new bootstrap.Modal(document.getElementById('offcanvasAddUser'));
        modal.show();
    }

    showEditUserModal(user) {
        console.log('Edit user:', user);
    }

    async handleAddUser() {
        const form = document.getElementById('addNewUserForm');
        const formData = new FormData(form);
        
        const userData = {
            username: formData.get('userFullname'),
            email: formData.get('userEmail'),
            phoneNumber: formData.get('userContact'),
            fullName: formData.get('companyName'),
        };

        try {
            const response = await fetch('http://localhost:5050/api/user', {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${this.getToken()}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(userData)
            });

            if (!response.ok) {
                throw new Error('Failed to create user');
            }

            const data = await response.json();
            
            if (data.success) {
                this.showSuccess('User created successfully');
                const modal = bootstrap.Modal.getInstance(document.getElementById('offcanvasAddUser'));
                modal.hide();
                form.reset();
                this.loadUsers();
                this.loadStatistics();
            } else {
                this.showError('Failed to create user: ' + data.message);
            }
        } catch (error) {
            console.error('Error creating user:', error);
            this.showError('Failed to create user');
        }
    }

    getToken() {
        return localStorage.getItem('token') || sessionStorage.getItem('token');
    }

    getRoleBadgeClass(role) {
        return role === 'Google' ? 'info' : 'primary';
    }

    getStatusBadgeClass(status) {
        switch (status) {
            case 'Active': return 'success';
            case 'Inactive': return 'secondary';
            case 'Suspended': return 'warning';
            case 'Banned': return 'danger';
            default: return 'secondary';
        }
    }

    getStatusText(status) {
        return status || 'Unknown';
    }

    formatDate(dateString) {
        return new Date(dateString).toLocaleDateString();
    }

    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    showSuccess(message) {
        console.log('Success:', message);
    }

    showError(message) {
        console.error('Error:', message);
    }
}

document.addEventListener('DOMContentLoaded', () => {
    window.userManagement = new UserManagement();
}); 