import { sanitizeHtml, isAuthenticated, logout } from './auth-utils.js';
import { fetchUsers, updateUser, deleteUser, logoutUser, restoreUser, statistics, getUserById, getUserByEmail, getUserByUsername } from './api.js';

const API_BASE_URL = 'http://localhost:5050';

let currentUser = null;
let authToken = localStorage.getItem('authToken') || null;
let allUsersCache = [];
let filteredUsersCache = [];
let currentPage = 1;
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
            await fetch(`${API_BASE_URL}/api/Auth/users/${id}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${authToken}` }
            });
            renderUserTable();
        } catch {}
    }
}

const GOOGLE_CLIENT_ID = '157841978934-fmgq60lshk9iq65s7h37mc7ta78m8nu3.apps.googleusercontent.com';
const GOOGLE_REDIRECT_URI = 'http://localhost:8080/login';
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
                    window.location.href = '/admin/app-user-list.html';
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
    const isAdminPage = window.location.pathname.startsWith('/admin/');
    const isLoginPage = window.location.pathname.endsWith('/login');
    if (isAdminPage && !isAuthenticated()) {
        window.location.href = '/admin/auth/login.html';
    }
    if (isLoginPage && isAuthenticated()) {
        window.location.href = '/admin/app-user-list.html';
    }
});

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
                window.location.href = '/login';
            }, 500);
        });
        logoutBtn.onclick = logoutBtn.onclick || function(e) {
            e.preventDefault();
            e.stopPropagation();
            localStorage.removeItem('authToken');
            sessionStorage.clear();
            currentUser = null;
            authToken = null;
            window.location.href = '/login';
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
    window.location.href = '/login';
}
function goToDashboard() {
    window.location.href = '/admin/app-user-list.html';
}

window.addEventListener('DOMContentLoaded', async function() {
    const userTableBody = document.querySelector('#user-table tbody');
    const searchInput = document.getElementById('user-search-input');
    const paginationContainer = document.getElementById('pagination-container');
    if (!userTableBody) return;
    let loadingRow = document.createElement('tr');
    loadingRow.innerHTML = '<td colspan="7" style="text-align:center;"><span class="loading-spinner"></span> Loading users...</td>';
    userTableBody.innerHTML = '';
    userTableBody.appendChild(loadingRow);
    try {
        const users = await fetchUsers();
        allUsersCache = users;
        filteredUsersCache = users;
        currentPage = 1;
        renderUserTableWithPagination(filteredUsersCache, currentPage);
    } catch (err) {
        userTableBody.innerHTML = '<tr><td colspan="7">Failed to load users.</td></tr>';
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
});

function renderUserTableWithPagination(users, page) {
    const userTableBody = document.querySelector('#user-table tbody');
    const paginationContainer = document.getElementById('pagination-container');
    if (!userTableBody) return;
    if (!users || users.length === 0) {
        userTableBody.innerHTML = '<tr><td colspan="10">No users found.</td></tr>';
        if (paginationContainer) paginationContainer.innerHTML = '';
        return;
    }
    const total = users.length;
    const totalPages = Math.ceil(total / USERS_PER_PAGE);
    if (page < 1) page = 1;
    if (page > totalPages) page = totalPages;
    const start = (page - 1) * USERS_PER_PAGE;
    const end = start + USERS_PER_PAGE;
    const pageUsers = users.slice(start, end);
    userTableBody.innerHTML = '';
    pageUsers.forEach((user, rowIdx) => {
        const tr = document.createElement('tr');
        let statusText = '';
        switch (user.status) {
            case 1: statusText = 'Active'; break;
            case 2: statusText = 'Inactive'; break;
            case 3: statusText = 'Suspended'; break;
            case 4: statusText = 'Banned'; break;
            default: statusText = sanitizeHtml(user.status || '');
        }
        tr.innerHTML = `
            <td>${sanitizeHtml(user.id)}</td>
            <td>${sanitizeHtml(user.username)}</td>
            <td>${sanitizeHtml(user.fullName || '-')}</td>
            <td>${sanitizeHtml(user.email)}</td>
            <td>${sanitizeHtml(user.phoneNumber || '-')}</td>
            <td>${statusText}</td>
            <td>${user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString('en-GB', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit' }) : 'Never'}</td>
            <td>
                <button class="btn btn-edit-user">Edit</button>
                <button class="btn btn-danger btn-delete-user">Delete</button>
            </td>
        `;
        Array.from(tr.children).forEach((td, idx) => {
            if (idx < tr.children.length - 1) {
                td.style.cursor = 'pointer';
                td.onclick = function(e) {
                    e.stopPropagation();
                    showUserDetailModal(user);
                };
            }
        });
        userTableBody.appendChild(tr);
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
                await deleteUser(user.id);
                toastr.success('User deleted successfully!');
                btn.closest('tr').remove();
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

window.addEventListener('DOMContentLoaded', function() {
    const modal = document.getElementById('edit-user-modal');
    const closeBtn = document.getElementById('close-edit-modal');
    if (modal && closeBtn) {
        closeBtn.onclick = function() {
            closeEditModal();
        };
        modal.onclick = function(event) {
            if (event.target === modal) {
                closeEditModal();
            }
        };
    }
    const editForm = document.getElementById('edit-user-form');
    if (editForm) {
        editForm.onsubmit = async function(e) {
            e.preventDefault();
            const userId = document.getElementById('edit-user-id').value;
            const username = document.getElementById('edit-username').value.trim();
            const fullName = document.getElementById('edit-fullname').value.trim();
            const email = document.getElementById('edit-email').value.trim();
            const phone = document.getElementById('edit-phone').value.trim();
            const dob = document.getElementById('edit-dob').value.trim();
            const address = document.getElementById('edit-address').value.trim();
            const bio = document.getElementById('edit-bio').value.trim();
            const status = document.getElementById('edit-status').value.trim();
            const isVerified = document.getElementById('edit-verified').checked;
            const saveBtn = editForm.querySelector('button[type="submit"]');
            saveBtn.disabled = true;
            saveBtn.textContent = 'Saving...';
            try {
                const updateData = {};
                if (fullName) updateData.fullName = fullName;
                if (phone) updateData.phoneNumber = phone;
                if (dob) updateData.dateOfBirth = dob;
                if (address) updateData.address = address;
                if (bio) updateData.bio = bio;
                if (status) updateData.status = parseInt(status);
                updateData.isVerified = isVerified;

                await updateUser(userId, updateData);
                toastr.success('User updated successfully!');
                closeEditModal();
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
            } catch (err) {
                toastr.error('Update failed: ' + (err.message || 'Unknown error'));
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

function editUser(userId) {
    const user = allUsersCache.find(u => u.id === userId);
    if (!user) {
        toastr.error('User not found');
        return;
    }

    document.getElementById('edit-username').value = user.username;
    document.getElementById('edit-fullname').value = user.fullName || '';
    document.getElementById('edit-phone').value = user.phoneNumber || '';
    document.getElementById('edit-dob').value = user.dateOfBirth ? user.dateOfBirth.split('T')[0] : '';
    document.getElementById('edit-address').value = user.address || '';
    document.getElementById('edit-bio').value = user.bio || '';
    document.getElementById('edit-status').value = user.status || 1;
    document.getElementById('edit-verified').checked = user.isVerified || false;

    document.getElementById('edit-user-modal').style.display = 'block';
}

function closeEditModal() {
    document.getElementById('edit-user-modal').style.display = 'none';
}

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

function showUserDetailModal(user) {
    const modal = document.getElementById('user-detail-modal');
    const content = document.getElementById('user-detail-content');
    content.innerHTML = `
      <div><b>ID:</b> ${sanitizeHtml(user.id)}</div>
      <div><b>Username:</b> ${sanitizeHtml(user.username)}</div>
      <div><b>Full Name:</b> ${sanitizeHtml(user.fullName || '-')}</div>
      <div><b>Email:</b> ${sanitizeHtml(user.email)}</div>
      <div><b>Phone:</b> ${sanitizeHtml(user.phoneNumber || '-')}</div>
      <div><b>Status:</b> ${(() => { switch(user.status){case 1: return 'Active';case 2: return 'Inactive';case 3: return 'Suspended';case 4: return 'Banned';default: return sanitizeHtml(user.status || '');}})()}</div>
      <div><b>Last Login:</b> ${user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString('en-GB', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit' }) : 'Never'}</div>
      <div><b>Address:</b> ${sanitizeHtml(user.address || '-')}</div>
      <div><b>Bio:</b> ${sanitizeHtml(user.bio || '-')}</div>
      <div><b>Date of Birth:</b> ${user.dateOfBirth ? new Date(user.dateOfBirth).toLocaleDateString('en-GB') : '-'}</div>
      <div><b>Provider:</b> ${sanitizeHtml(user.loginProvider || '-')}</div>
      <div><b>Profile Picture:</b> ${user.profilePicture ? `<img src='${user.profilePicture}' alt='avatar' style='max-width:60px;max-height:60px;border-radius:50%;'/>` : '-'}</div>
    `;
    modal.style.display = 'block';
}
document.getElementById('close-user-detail-modal').onclick = function() {
    document.getElementById('user-detail-modal').style.display = 'none';
};
document.getElementById('user-detail-modal').onclick = function(e) {
    if (e.target === this) this.style.display = 'none';
}; 