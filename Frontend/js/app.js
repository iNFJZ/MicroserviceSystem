import { sanitizeHtml, isAuthenticated, logout, sanitizeInput, isValidEmail } from './auth-utils.js';
import { fetchUsers, fetchDeletedUsers, updateUser, deleteUser, logoutUser, restoreUser, statistics, getUserById, getUserByEmail, getUserByUsername } from './api.js';

if (typeof toastr !== 'undefined') {
    toastr.options = {
        "closeButton": true,
        "debug": false,
        "newestOnTop": false,
        "progressBar": true,
        "positionClass": "toast-top-right",
        "preventDuplicates": false,
        "onclick": null,
        "showDuration": "300",
        "hideDuration": "1000",
        "timeOut": "5000",
        "extendedTimeOut": "1000",
        "showEasing": "swing",
        "hideEasing": "linear",
        "showMethod": "fadeIn",
        "hideMethod": "fadeOut"
    };
}

const API_BASE_URL = 'http://localhost:5050';

let currentUser = null;
let authToken = localStorage.getItem('authToken') || null;
let allUsersCache = [];
let filteredUsersCache = [];
let currentPage = 1;
let allDeletedUsersCache = [];
let filteredDeletedUsersCache = [];
let currentDeletedPage = 1;
const USERS_PER_PAGE = 10;

if (window.self !== window.top) {
    window.top.location = window.self.location;
}

function showMessage(elementId, message, isError = false) {
    if (isError) {
        toastr.error(message);
    } else {
        toastr.success(message);
    }
    
    const element = document.getElementById(elementId);
    if (element) {
        const sanitizedMessage = sanitizeHtml(message);
        element.textContent = sanitizedMessage;
        element.className = `form-message ${isError ? 'error' : 'success'}`;
    }
}

window.toggleUserStatus = async function(id, isActive) {
    try {
        await fetch(`${API_BASE_URL}/api/User/${id}/status`, {
            method: 'PATCH',
            headers: {
                'Authorization': `Bearer ${authToken}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ isActive: !isActive })
        });
        renderUserTable();
    } catch {}
};

window.deleteUser = async function(id) {
    if (confirm('Are you sure you want to delete this user?')) {
        try {
            const res = await deleteUser(id);
            toastr.success('User deleted successfully!');
            if (res && res.message && res.message.includes('deactivated successfully')) {
                toastr.info('A deactivation email has been sent to the user.');
            }
            renderUserTable();
        } catch (error) {
            toastr.error('Failed to delete user: ' + (error.message || 'Unknown error'));
        }
    }
}

const GOOGLE_CLIENT_ID = '157841978934-fmgq60lshk9iq65s7h37mc7ta78m8nu3.apps.googleusercontent.com';
const GOOGLE_REDIRECT_URI = 'http://localhost:8080/';
const GOOGLE_SCOPE = 'openid email profile';
const GOOGLE_AUTH_URL =
    'https://accounts.google.com/o/oauth2/v2/auth' +
    '?response_type=code' +
    `&client_id=${encodeURIComponent(GOOGLE_CLIENT_ID)}` +
    `&redirect_uri=${encodeURIComponent(GOOGLE_REDIRECT_URI)}` +
    `&scope=${encodeURIComponent(GOOGLE_SCOPE)}` +
    '&access_type=offline' +
    '&prompt=select_account';

window.loginWithGoogle = function() {
    window.location.href = GOOGLE_AUTH_URL;
};

window.addEventListener('DOMContentLoaded', async () => {
    const urlParams = new URLSearchParams(window.location.search);
    const code = urlParams.get('code');
    const path = window.location.pathname;
    if (code && path.endsWith('/login')) {
        const msg = document.getElementById('login-message');
        if (msg) msg.textContent = 'Logging in with Google...';
        let success = false;
        try {
            const requestBody = { code, redirectUri: GOOGLE_REDIRECT_URI };
            const res = await fetch(`${API_BASE_URL}/api/Auth/login/google`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(requestBody)
            });
            const data = await res.json();
            if (res.ok && data.token) {
                localStorage.setItem('authToken', data.token);
                toastr.success('Google login successful! Redirecting...');
                setTimeout(() => {
                    window.location.href = '/admin/';
                }, 1000);
                success = true;
            } else {
                const errorMsg = data.message || 'Google login failed!';
                if (msg) msg.textContent = errorMsg;
                toastr.error(errorMsg);
            }
        } catch (err) {
            const errorMsg = 'Google login failed!';
            if (msg) msg.textContent = errorMsg;
            toastr.error(errorMsg);
        }
        if (!success && msg) {
            msg.textContent = 'Google login failed!';
        }
    }
});

document.addEventListener('DOMContentLoaded', function() {
    setupLogoutHandlers();
});

(function() {
    const isLoginPage = window.location.pathname.endsWith('/login') || window.location.pathname.endsWith('/auth/login.html');
    const token = localStorage.getItem('authToken');
    if (!isLoginPage && !token) {
        window.location.href = '/auth/login.html';
    }
})();

function setupLogoutHandlers() {
    const logoutBtn = document.getElementById('logout-btn');
    if (!logoutBtn) return;
    if (logoutBtn && !logoutBtn.hasAttribute('data-logout-handler')) {
        logoutBtn.setAttribute('data-logout-handler', 'true');
        logoutBtn.addEventListener('click', async function(e) {
            e.preventDefault();
            e.stopPropagation();
            logoutBtn.disabled = true;
            logoutBtn.textContent = 'Logging out...';
            try {
                await logoutUser();
            } catch (error) {
            }
            localStorage.removeItem('authToken');
            sessionStorage.clear();
            currentUser = null;
            authToken = null;
            toastr.success('Logged out successfully!');
            setTimeout(() => {
                window.location.href = '/auth/login.html';
            }, 500);
        });
        logoutBtn.onclick = logoutBtn.onclick || function(e) {
            e.preventDefault();
            e.stopPropagation();
            localStorage.removeItem('authToken');
            sessionStorage.clear();
            currentUser = null;
            authToken = null;
            window.location.href = '/auth/login.html';
        };
    }
}
window.setupLogoutHandlers = setupLogoutHandlers;

document.addEventListener('DOMContentLoaded', function() {
    setupLogoutHandlers();
    
    const observer = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.type === 'childList') {
                setupLogoutHandlers();
            }
        });
    });
    
    observer.observe(document.body, {
        childList: true,
        subtree: true
    });
});

setTimeout(setupLogoutHandlers, 100);
setTimeout(setupLogoutHandlers, 500);
setTimeout(setupLogoutHandlers, 1000);

function goToLogin() {
    window.location.href = '/auth/login.html';
}
function goToDashboard() {
    window.location.href = '/admin/';
}

window.addEventListener('DOMContentLoaded', async function() {
    try {
        await loadActiveUsers();
        await loadDeletedUsers();
        await updateUserStats();
        setupTabSwitching();
        setupLogoutHandlers();
    } catch (error) {
        console.error('Error initializing app:', error);
        toastr.error('Failed to initialize application');
    }
});

async function loadActiveUsers() {
    const userTableBody = document.querySelector('#user-table tbody');
    const searchInput = document.getElementById('user-search-input');
    const paginationContainer = document.getElementById('pagination-container');
    if (!userTableBody) return;
    
    let loadingRow = document.createElement('tr');
    loadingRow.innerHTML = '<td colspan="8" style="text-align:center;"><span class="loading-spinner"></span> Loading users...</td>';
    userTableBody.innerHTML = '';
    userTableBody.appendChild(loadingRow);
    
    try {
        const users = await fetchUsers();
        allUsersCache = users;
        filteredUsersCache = users;
        currentPage = 1;
        renderUserTableWithPagination(filteredUsersCache, currentPage);
    } catch (err) {
        userTableBody.innerHTML = '<tr><td colspan="8">Failed to load users.</td></tr>';
        toastr.error('Failed to load users: ' + (err.message || 'Unknown error'));
    }
    
    if (searchInput) {
        searchInput.addEventListener('input', function() {
            const keyword = this.value.trim().toLowerCase();
            filteredUsersCache = !keyword ? allUsersCache : allUsersCache.filter(u =>
                (u.username && u.username.toLowerCase().includes(keyword)) ||
                (u.email && u.email.toLowerCase().includes(keyword)) ||
                (u.fullName && u.fullName.toLowerCase().includes(keyword))
            );
            currentPage = 1;
            renderUserTableWithPagination(filteredUsersCache, currentPage);
        });
    }
    
    if (paginationContainer) {
        paginationContainer.onclick = function(e) {
            if (e.target.classList.contains('page-link')) {
                const page = parseInt(e.target.getAttribute('data-page'));
                if (!isNaN(page)) {
                    currentPage = page;
                    renderUserTableWithPagination(filteredUsersCache, currentPage);
                }
            }
        };
    }
}

async function loadDeletedUsers() {
    const deletedUserTableBody = document.querySelector('#deleted-user-table tbody');
    const deletedSearchInput = document.getElementById('deleted-user-search-input');
    const deletedPaginationContainer = document.getElementById('deleted-pagination-container');
    if (!deletedUserTableBody) return;
    
    let loadingRow = document.createElement('tr');
    loadingRow.innerHTML = '<td colspan="8" style="text-align:center;"><span class="loading-spinner"></span> Loading deleted users...</td>';
    deletedUserTableBody.innerHTML = '';
    deletedUserTableBody.appendChild(loadingRow);
    
    try {
        const deletedUsers = await fetchDeletedUsers();
        allDeletedUsersCache = deletedUsers;
        filteredDeletedUsersCache = deletedUsers;
        currentDeletedPage = 1;
        renderDeletedUserTableWithPagination(filteredDeletedUsersCache, currentDeletedPage);
    } catch (err) {
        deletedUserTableBody.innerHTML = '<tr><td colspan="8">Failed to load deleted users.</td></tr>';
        toastr.error('Failed to load deleted users: ' + (err.message || 'Unknown error'));
    }
    
    if (deletedSearchInput) {
        deletedSearchInput.addEventListener('input', function() {
            const keyword = this.value.trim().toLowerCase();
            filteredDeletedUsersCache = !keyword ? allDeletedUsersCache : allDeletedUsersCache.filter(u =>
                (u.username && u.username.toLowerCase().includes(keyword)) ||
                (u.email && u.email.toLowerCase().includes(keyword)) ||
                (u.fullName && u.fullName.toLowerCase().includes(keyword))
            );
            currentDeletedPage = 1;
            renderDeletedUserTableWithPagination(filteredDeletedUsersCache, currentDeletedPage);
        });
    }
    
    if (deletedPaginationContainer) {
        deletedPaginationContainer.onclick = function(e) {
            if (e.target.classList.contains('page-link')) {
                const page = parseInt(e.target.getAttribute('data-page'));
                if (!isNaN(page)) {
                    currentDeletedPage = page;
                    renderDeletedUserTableWithPagination(filteredDeletedUsersCache, currentDeletedPage);
                }
            }
        };
    }
}

function setupTabSwitching() {
    const activeTab = document.getElementById('active-users-tab');
    const deletedTab = document.getElementById('deleted-users-tab');
    
    if (activeTab) {
        activeTab.addEventListener('click', function() {
            loadActiveUsers();
        });
    }
    
    if (deletedTab) {
        deletedTab.addEventListener('click', function() {
            loadDeletedUsers();
        });
    }
}

function renderUserTableWithPagination(users, page) {
    const userTableBody = document.querySelector('#user-table tbody');
    const paginationContainer = document.getElementById('pagination-container');
    if (!userTableBody) return;
    if (!users || users.length === 0) {
        userTableBody.innerHTML = '<tr><td colspan="8">No users found.</td></tr>';
        if (paginationContainer) paginationContainer.innerHTML = '';
        return;
    }
    const total = users.length;
    const totalPages = Math.ceil(total / USERS_PER_PAGE);
    const startIndex = (page - 1) * USERS_PER_PAGE;
    const endIndex = startIndex + USERS_PER_PAGE;
    const pageUsers = users.slice(startIndex, endIndex);
    userTableBody.innerHTML = pageUsers.map((user, idx) => `
        <tr class="user-row" data-user-idx="${idx}">
            <td>${sanitizeHtml(user.id)}</td>
            <td>${sanitizeHtml(user.username)}</td>
            <td>${sanitizeHtml(user.fullName || '-')}</td>
            <td>${sanitizeHtml(user.email)}</td>
            <td>${sanitizeHtml(user.phoneNumber || '-')}</td>
            <td>${
                Number(user.status) === 1
                    ? '<span class="badge bg-success">Active</span>'
                    : Number(user.status) === 2
                    ? '<span class="badge bg-secondary">Inactive</span>'
                    : Number(user.status) === 3
                    ? '<span class="badge bg-warning">Suspended</span>'
                    : Number(user.status) === 4
                    ? '<span class="badge bg-danger">Banned</span>'
                    : '<span class="badge bg-secondary">Unknown</span>'
            }</td>
            <td>${user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString('en-GB', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit' }) : '-'}</td>
            <td>
                <button class="btn btn-sm btn-primary btn-edit-user" style="min-width:80px" data-user-idx="${idx}">Edit</button>
                <button class="btn btn-sm btn-danger btn-delete-user" style="min-width:80px" data-user-idx="${idx}">Delete</button>
            </td>
        </tr>
    `).join('');
    userTableBody.querySelectorAll('tr.user-row').forEach((row, idx) => {
        row.addEventListener('click', function(e) {
            if (e.target.closest('button')) return;
            showUserDetailModal(pageUsers[idx]);
        });
    });
    document.querySelectorAll('.btn-edit-user').forEach((btn, idx) => {
        btn.onclick = function(e) {
            e.preventDefault();
            const user = pageUsers[idx];
            editUser(user.id);
        };
    });
    document.querySelectorAll('.btn-delete-user').forEach((btn, idx) => {
        btn.onclick = async function(e) {
            e.preventDefault();
            const user = pageUsers[idx];
            if (!confirm('Are you sure you want to delete this user?')) return;
            btn.disabled = true;
            btn.textContent = 'Deleting...';
            try {
                const res = await deleteUser(user.id);
                toastr.success('User deleted successfully!');
                if (res && res.message && res.message.includes('deactivated successfully')) {
                    toastr.info('A deactivation email has been sent to the user.');
                }
                allUsersCache = [];
                filteredUsersCache = [];
                allDeletedUsersCache = [];
                filteredDeletedUsersCache = [];
                currentPage = 1;
                const searchInput = document.getElementById('user-search-input');
                if (searchInput) searchInput.value = '';
                await loadActiveUsers();
                await loadDeletedUsers();
                await updateUserStats();
            } catch (err) {
                toastr.error('Delete failed: ' + (err.message || 'Unknown error'));
                btn.disabled = false;
                btn.textContent = 'Delete';
            }
        };
    });
    if (paginationContainer) {
        let html = '';
        if (totalPages > 1) {
            html += `<button class="page-link" data-page="${page - 1}" ${page === 1 ? 'disabled' : ''}>Previous</button>`;
            for (let i = 1; i <= totalPages; i++) {
                html += `<button class="page-link${i === page ? ' active' : ''}" data-page="${i}">${i}</button>`;
            }
            html += `<button class="page-link" data-page="${page + 1}" ${page === totalPages ? 'disabled' : ''}>Next</button>`;
            html += `<span class="pagination-info"> Page ${page} of ${totalPages} | Total: ${total} users</span>`;
        }
        paginationContainer.innerHTML = html;
    }
}

function renderDeletedUserTableWithPagination(users, page) {
    const userTableBody = document.querySelector('#deleted-user-table tbody');
    const paginationContainer = document.getElementById('deleted-pagination-container');
    if (!userTableBody) return;
    if (!users || users.length === 0) {
        userTableBody.innerHTML = '<tr><td colspan="8">No deleted users found.</td></tr>';
        if (paginationContainer) paginationContainer.innerHTML = '';
        return;
    }
    const total = users.length;
    const totalPages = Math.ceil(total / USERS_PER_PAGE);
    const startIndex = (page - 1) * USERS_PER_PAGE;
    const endIndex = startIndex + USERS_PER_PAGE;
    const pageUsers = users.slice(startIndex, endIndex);
    userTableBody.innerHTML = pageUsers.map(user => `
        <tr class="deleted-user-row">
            <td>${sanitizeHtml(user.id)}</td>
            <td>${sanitizeHtml(user.username)}</td>
            <td>${sanitizeHtml(user.fullName || '-')}</td>
            <td>${sanitizeHtml(user.email)}</td>
            <td>${sanitizeHtml(user.phoneNumber || '-')}</td>
            <td><span class="badge bg-danger">Deleted</span></td>
            <td>${user.deletedAt ? new Date(user.deletedAt).toLocaleString('en-GB', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' }) : '-'}</td>
            <td><button class="btn btn-sm btn-success btn-restore-user">Restore</button></td>
        </tr>
    `).join('');
    userTableBody.querySelectorAll('tr.deleted-user-row').forEach((row, idx) => {
        row.addEventListener('click', function(e) {
            if (e.target.closest('button')) return;
            showUserDetailModal(pageUsers[idx]);
        });
    });
    document.querySelectorAll('.btn-restore-user').forEach((btn, idx) => {
        btn.onclick = async function(e) {
            e.preventDefault();
            const user = pageUsers[idx];
            if (confirm(`Are you sure you want to restore user "${user.username}"?`)) {
                btn.disabled = true;
                btn.textContent = 'Restoring...';
                try {
                    await restoreUser(user.id);
                    toastr.success('User restored successfully!');
                    allUsersCache = [];
                    filteredUsersCache = [];
                    allDeletedUsersCache = [];
                    filteredDeletedUsersCache = [];
                    currentPage = 1;
                    const searchInput = document.getElementById('user-search-input');
                    if (searchInput) searchInput.value = '';
                    const deletedSearchInput = document.getElementById('deleted-user-search-input');
                    if (deletedSearchInput) deletedSearchInput.value = '';
                    await loadActiveUsers();
                    await loadDeletedUsers();
                    await updateUserStats();
                } catch (err) {
                    toastr.error('Restore failed: ' + (err.message || 'Unknown error'));
                    btn.disabled = false;
                    btn.textContent = 'Restore';
                }
            }
        };
    });
    if (paginationContainer) {
        let html = '';
        if (totalPages > 1) {
            html += `<button class="page-link" data-page="${page - 1}" ${page === 1 ? 'disabled' : ''}>Previous</button>`;
            for (let i = 1; i <= totalPages; i++) {
                html += `<button class="page-link${i === page ? ' active' : ''}" data-page="${i}">${i}</button>`;
            }
            html += `<button class="page-link" data-page="${page + 1}" ${page === totalPages ? 'disabled' : ''}>Next</button>`;
            html += `<span class="pagination-info"> Page ${page} of ${totalPages} | Total: ${total} deleted users</span>`;
        }
        paginationContainer.innerHTML = html;
    }
}

window.addEventListener('DOMContentLoaded', function() {
    const modal = document.getElementById('edit-user-modal');
    const closeBtn = document.getElementById('close-edit-modal');
    const closeBtnAlt = document.getElementById('close-edit-modal-btn');
    
    if (modal && closeBtn) {
        closeBtn.onclick = function(e) {
            e.preventDefault();
            e.stopPropagation();
            closeEditModal();
        };
        
        if (closeBtnAlt) {
            closeBtnAlt.onclick = function(e) {
                e.preventDefault();
                e.stopPropagation();
                closeEditModal();
            };
        }
        
        modal.addEventListener('click', function(event) {
            if (event.target === modal) {
                closeEditModal();
            }
        });
        
        const modalDialog = modal.querySelector('.modal-dialog');
        if (modalDialog) {
            modalDialog.addEventListener('click', function(event) {
                event.stopPropagation();
            });
            
            const form = modalDialog.querySelector('form');
            if (form) {
                form.addEventListener('click', function(event) {
                    event.stopPropagation();
                });
            }
            
            const modalContent = modalDialog.querySelector('.modal-content');
            if (modalContent) {
                modalContent.addEventListener('click', function(event) {
                    event.stopPropagation();
                });
            }
        }
        
        document.addEventListener('keydown', function(event) {
            if (event.key === 'Escape' && modal.classList.contains('show')) {
                closeEditModal();
            }
        });
    }
    
    const editForm = document.getElementById('edit-user-form');
    if (editForm) {
        editForm.onsubmit = async function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            const userId = document.getElementById('edit-user-id').value;
            const username = document.getElementById('edit-username').value.trim();
            const fullName = sanitizeInput(document.getElementById('edit-fullname').value.trim());
            const email = sanitizeInput(document.getElementById('edit-email').value.trim());
            const phone = document.getElementById('edit-phone').value.trim();
            const dob = document.getElementById('edit-dob').value.trim();
            const address = sanitizeInput(document.getElementById('edit-address').value.trim());
            const bio = sanitizeInput(document.getElementById('edit-bio').value.trim());
            const status = document.getElementById('edit-status').value.trim();
            const isVerified = false;

            const errors = [];
            if (!fullName) {
                errors.push('Full name is required');
            } else if (!/^[a-zA-ZÀ-ỹ\s]+$/.test(fullName)) {
                errors.push('Full name can only contain letters, spaces, and Vietnamese characters');
            }
            if (phone && !/^[0-9]{10,11}$/.test(phone)) {
                errors.push('Phone number must be 10-11 digits and contain only numbers.');
            }
            if (address && address.length > 200) {
                errors.push('Address is too long (max 200 characters)');
            }
            if (bio && bio.length > 500) {
                errors.push('Bio is too long (max 500 characters)');
            }
            if (!status || !['1','2','3','4'].includes(status)) {
                errors.push('Status is required and must be one of: Active, Inactive, Suspended, Banned');
            }
            if (errors.length > 0) {
                toastr.error(errors.filter(Boolean).join('<br>'));
                return;
            }
            
            const saveBtn = editForm.querySelector('button[type="submit"]');
            saveBtn.disabled = true;
            saveBtn.textContent = 'Saving...';
            
            try {
                const updateData = {};
                if (fullName) updateData.fullName = fullName;
                if (phone) updateData.phoneNumber = phone;
                if (dob) {
                    const dateObj = new Date(dob);
                    updateData.dateOfBirth = dateObj.toISOString();
                }
                if (address) updateData.address = address;
                if (bio) updateData.bio = bio;
                if (status) updateData.status = parseInt(status);
                updateData.isVerified = isVerified;

                await updateUser(userId, updateData);
                toastr.success('User updated successfully!');
                closeEditModal();
                
                const oldUser = allUsersCache.find(u => u.id === userId);
                if (oldUser && (oldUser.isVerified !== isVerified || oldUser.status !== parseInt(status))) {
                    allUsersCache = [];
                    filteredUsersCache = [];
                    allDeletedUsersCache = [];
                    filteredDeletedUsersCache = [];
                    const searchInput = document.getElementById('user-search-input');
                    if (searchInput) searchInput.value = '';
                    await loadActiveUsers();
                } else {
                    const idx = allUsersCache.findIndex(u => u.id === userId);
                    if (idx !== -1) {
                        allUsersCache[idx].fullName = fullName;
                        allUsersCache[idx].phoneNumber = phone;
                        allUsersCache[idx].dateOfBirth = dob ? dob + 'T00:00:00' : null;
                        allUsersCache[idx].address = address;
                        allUsersCache[idx].bio = bio;
                        allUsersCache[idx].status = status ? parseInt(status) : 1;
                        allUsersCache[idx].isVerified = isVerified;
                        renderUserTableWithPagination(filteredUsersCache, currentPage);
                    }
                }
                
                await updateUserStats();
            } catch (err) {
                let msg = 'Update failed: ';
                if (err && err.response && err.response.data && err.response.data.message) {
                    msg += err.response.data.message;
                } else if (err && err.message) {
                    msg += err.message;
                } else {
                    msg += 'Unknown error';
                }
                toastr.error(msg);
            } finally {
                saveBtn.disabled = false;
                saveBtn.textContent = 'Save Changes';
            }
        };
    }
});

window.addEventListener('DOMContentLoaded', async function() {
    const userStatsContainer = document.getElementById('user-stats-container');
    if (userStatsContainer) {
        try {
            const users = await fetchUsers();
            const total = users.length;
            const verified = users.filter(u => u.isVerified).length;
            const unverified = total - verified;
            userStatsContainer.innerHTML = `
                <div class="stat-item"><span class="stat-label">Total Users:</span> <span class="stat-value">${total}</span></div>
                <div class="stat-item"><span class="stat-label">Verified:</span> <span class="stat-value">${verified}</span></div>
                <div class="stat-item"><span class="stat-label">Unverified:</span> <span class="stat-value">${unverified}</span></div>
            `;
        } catch (err) {
            userStatsContainer.innerHTML = '<span class="stat-error">Failed to load user stats.</span>';
        }
    }
});

function preventBodyScroll() {
    document.body.style.overflow = 'hidden';
    document.body.style.paddingRight = '0px';
}

function restoreBodyScroll() {
    document.body.style.overflow = '';
    document.body.style.paddingRight = '';
}

let activeModal = null;

function setActiveModal(modal) {
    if (activeModal && activeModal !== modal) {
        activeModal.classList.remove('show');
        setTimeout(() => {
            activeModal.style.display = 'none';
        }, 300);
    }
    activeModal = modal;
}

function clearActiveModal() {
    activeModal = null;
}

function editUser(userId) {
    const user = allUsersCache.find(u => u.id === userId);
    if (!user) {
        toastr.error('User not found');
        return;
    }

    document.getElementById('edit-user-id').value = user.id;
    document.getElementById('edit-username').value = user.username;
    document.getElementById('edit-email').value = user.email || '';
    document.getElementById('edit-fullname').value = user.fullName || '';
    document.getElementById('edit-phone').value = user.phoneNumber || '';
    document.getElementById('edit-dob').value = user.dateOfBirth ? user.dateOfBirth.split('T')[0] : '';
    document.getElementById('edit-address').value = user.address || '';
    document.getElementById('edit-bio').value = user.bio || '';
    let statusVal = [1,2,3,4].includes(Number(user.status)) ? String(user.status) : "2";
    document.getElementById('edit-status').value = statusVal;

    const modal = document.getElementById('edit-user-modal');
    if (!modal) {
        toastr.error('Modal not found');
        return;
    }

    setActiveModal(modal);
    modal.style.display = 'block';
    
    setTimeout(() => {
        modal.classList.add('show');
        preventBodyScroll();
        
        const firstInput = modal.querySelector('input:not([disabled])');
        if (firstInput) {
            firstInput.focus();
        }
    }, 50);
}

function closeEditModal() {
    const modal = document.getElementById('edit-user-modal');
    if (!modal) return;
    
    modal.classList.remove('show');
    
    restoreBodyScroll();
    
    clearActiveModal();
    
    setTimeout(() => {
        modal.style.display = 'none';
        
        const form = document.getElementById('edit-user-form');
        if (form) {
            form.reset();
        }
    }, 300);
}

function showUserDetailModal(user) {
    const modal = document.getElementById('user-detail-modal');
    const content = document.getElementById('user-detail-content');
    let deletedAtRow = '';
    if (user.deletedAt) {
        deletedAtRow = `<div><b>Deleted At:</b> ${new Date(user.deletedAt).toLocaleString('en-GB', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })}</div>`;
    }
    content.innerHTML = `
      <div><b>ID:</b> ${sanitizeHtml(user.id)}</div>
      <div><b>Username:</b> ${sanitizeHtml(user.username)}</div>
      <div><b>Full Name:</b> ${sanitizeHtml(user.fullName || '-')}</div>
      <div><b>Email:</b> ${sanitizeHtml(user.email)}</div>
      <div><b>Phone:</b> ${sanitizeHtml(user.phoneNumber || '-')}</div>
      <div><b>Status:</b> ${(() => { switch(user.status){case 1: return 'Active';case 2: return 'Inactive';case 3: return 'Suspended';case 4: return 'Banned';default: return sanitizeHtml(user.status || '');}})()}</div>
      <div><b>Last Login:</b> ${user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString('en-GB', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit' }) : 'Never'}</div>
      ${deletedAtRow}
      <div><b>Address:</b> ${sanitizeHtml(user.address || '-')}</div>
      <div><b>Bio:</b> ${sanitizeHtml(user.bio || '-')}</div>
      <div><b>Date of Birth:</b> ${user.dateOfBirth ? new Date(user.dateOfBirth).toLocaleDateString('en-GB') : '-'}</div>
      <div><b>Provider:</b> ${sanitizeHtml(user.loginProvider || '-')}</div>
      <div><b>Profile Picture:</b> ${user.profilePicture ? `<img src='${user.profilePicture}' alt='avatar' style='max-width:60px;max-height:60px;border-radius:50%;'/>` : '-'}</div>
    `;
    setActiveModal(modal);
    modal.style.display = 'block';
    modal.classList.add('show');
    preventBodyScroll();
}

function closeUserDetailModal() {
    const modal = document.getElementById('user-detail-modal');
    modal.classList.remove('show');
    restoreBodyScroll();
    clearActiveModal();
    setTimeout(() => {
        modal.style.display = 'none';
    }, 300);
}

window.addEventListener('DOMContentLoaded', function() {
    const userDetailModal = document.getElementById('user-detail-modal');
    const closeUserDetailBtn = document.getElementById('close-user-detail-modal');
    
    if (userDetailModal && closeUserDetailBtn) {
        closeUserDetailBtn.onclick = function(e) {
            e.preventDefault();
            e.stopPropagation();
            closeUserDetailModal();
        };
        
        userDetailModal.addEventListener('click', function(event) {
            if (event.target === userDetailModal) {
                closeUserDetailModal();
            }
        });
        
        const modalDialog = userDetailModal.querySelector('.modal-dialog');
        if (modalDialog) {
            modalDialog.addEventListener('click', function(event) {
                event.stopPropagation();
            });
        }
        
        document.addEventListener('keydown', function(event) {
            if (event.key === 'Escape' && userDetailModal.classList.contains('show')) {
                closeUserDetailModal();
            }
        });
    }
});

window.addEventListener('DOMContentLoaded', function() {
    const usernameElem = document.getElementById('current-username');
    if (usernameElem) {
        try {
            const token = localStorage.getItem('authToken');
            if (token) {
                const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
                if (payload && payload.username) {
                    usernameElem.textContent = payload.username;
                } else if (payload && payload.fullName) {
                    usernameElem.textContent = payload.fullName;
                } else if (payload && payload.email) {
                    usernameElem.textContent = payload.email;
                } else {
                    usernameElem.textContent = 'User';
                }
            } else {
                usernameElem.textContent = 'User';
            }
        } catch {
            usernameElem.textContent = 'User';
        }
    }
});

async function updateUserStats() {
    const userStatsContainer = document.getElementById('user-stats-container');
    if (userStatsContainer) {
        try {
            const users = await fetchUsers();
            const total = users.length;
            const verified = users.filter(u => u.isVerified).length;
            const unverified = total - verified;
            userStatsContainer.innerHTML = `
                <div class="stat-item"><span class="stat-label">Total Users:</span> <span class="stat-value">${total}</span></div>
                <div class="stat-item"><span class="stat-label">Verified:</span> <span class="stat-value">${verified}</span></div>
                <div class="stat-item"><span class="stat-label">Unverified:</span> <span class="stat-value">${unverified}</span></div>
            `;
        } catch (err) {
            userStatsContainer.innerHTML = '<span class="stat-error">Failed to load user stats.</span>';
        }
    }
} 