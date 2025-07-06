import { sanitizeHtml, sanitizeInput, isValidEmail, isValidPassword, isValidUsername } from '/shared/utils.js';
import { getToken, setToken, removeToken, isAuthenticated, getCurrentUser, logout } from '/shared/auth.js';
import { fetchUsers, deleteUser, updateUser, apiRequest, logoutUser } from '/shared/api.js';

const API_BASE_URL = 'http://localhost:5050';

const sections = {
    login: document.getElementById('login-section'),
    register: document.getElementById('register-section'),
    home: document.getElementById('home-section'),
    users: document.getElementById('users-section'),
    verifyEmail: document.getElementById('verify-email-section')
};
const navs = {
    login: document.getElementById('nav-login'),
    register: document.getElementById('nav-register'),
    home: document.getElementById('nav-home'),
    users: document.getElementById('nav-users'),
    logout: document.getElementById('nav-logout')
};

function showSection(name) {
    Object.values(sections).forEach(sec => sec.classList.remove('active'));
    sections[name].classList.add('active');
}
function updateNavbar(isLoggedIn) {
    navs.login.style.display = isLoggedIn ? 'none' : '';
    navs.register.style.display = isLoggedIn ? 'none' : '';
    navs.home.style.display = isLoggedIn ? '' : 'none';
    navs.users.style.display = isLoggedIn ? '' : 'none';
    navs.logout.style.display = isLoggedIn ? '' : 'none';
}

let currentUser = null;
let authToken = localStorage.getItem('authToken') || null;
let allUsersCache = [];
let filteredUsersCache = [];
let currentPage = 1;
const USERS_PER_PAGE = 10;

function parseJwt(token) {
    try {
        if (!token || typeof token !== 'string') {
            return {};
        }
        
        const parts = token.split('.');
        if (parts.length !== 3) {
            return {};
        }
        
        const base64Url = parts[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
        return JSON.parse(jsonPayload);
    } catch {
        return {};
    }
}

if (window.self !== window.top) {
    window.top.location = window.self.location;
}

function showMessage(elementId, message, isError = false) {
    if (isError) {
        toast.error(message);
    } else {
        toast.success(message);
    }
    
    const element = document.getElementById(elementId);
    if (element) {
        const sanitizedMessage = sanitizeHtml(message);
        element.textContent = sanitizedMessage;
        element.className = `form-message ${isError ? 'error' : 'success'}`;
    }
}

if (document.getElementById('login-form')) {
    const loginForm = document.getElementById('login-form');
    loginForm.onsubmit = async function(e) {
        e.preventDefault();
        const email = sanitizeInput(document.getElementById('login-email').value);
        const password = document.getElementById('login-password').value;
        
        const errors = [];
        
        if (!email) {
            errors.push('Email is required');
        } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
            errors.push('Please enter a valid email address');
        }
        
        if (!password) {
            errors.push('Password is required');
        } else if (password.length < 6) {
            errors.push('Password must be at least 6 characters');
        }
        
        if (errors.length > 0) {
            showMessage('login-message', errors.join(', '), true);
            return;
        }
        
        try {
            const res = await fetch(`${API_BASE_URL}/api/Auth/login`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, password })
            });
            const data = await res.json();
            if (res.ok && data.token) {
                authToken = data.token;
                localStorage.setItem('authToken', authToken);
                currentUser = parseJwt(authToken);
                toast.success('Login successful! Redirecting...', 1000);
                setTimeout(() => {
                    window.location.href = '/admin/dashboard.html';
                }, 1000);
            } else {
                if (data.errors && Array.isArray(data.errors)) {
                    showMessage('login-message', data.errors.join(', '), true);
                } else {
                    showMessage('login-message', data.message || 'Login failed!', true);
                }
            }
        } catch (err) {
            toast.error('Login failed!');
        }
    };
}

if (document.getElementById('register-form')) {
    const registerForm = document.getElementById('register-form');
    registerForm.onsubmit = async function(e) {
        e.preventDefault();
        const username = sanitizeInput(document.getElementById('register-username').value);
        const fullName = sanitizeInput(document.getElementById('register-fullname').value);
        const email = sanitizeInput(document.getElementById('register-email').value);
        const password = document.getElementById('register-password').value;
        
        const errors = [];
        
        if (!username) {
            errors.push('Username is required');
        } else if (!isValidUsername(username)) {
            errors.push('Username must be between 3 and 50 characters and contain only letters and numbers');
        }
        
        if (!email) {
            errors.push('Email is required');
        } else if (!isValidEmail(email)) {
            errors.push('Please enter a valid email address');
        }
        
        if (!password) {
            errors.push('Password is required');
        } else if (!isValidPassword(password)) {
            errors.push('Password must be at least 6 characters and contain at least one uppercase letter, one lowercase letter, and one number');
        }
        
        if (fullName && !/^[a-zA-ZÀ-ỹ\s]+$/.test(fullName)) {
            errors.push('Full name can only contain letters, spaces, and Vietnamese characters');
        }
        
        if (errors.length > 0) {
            showMessage('register-message', errors.join(', '), true);
            return;
        }
        
        try {
            const res = await fetch(`${API_BASE_URL}/api/Auth/register`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ 
                    username, 
                    fullName: fullName || null, 
                    email, 
                    password 
                })
            });
            const data = await res.json();
            if (res.ok) {
                toast.success('Registration successful! Please check your email to verify your account.');
                registerForm.reset();
            } else {
                if (data.errors && Array.isArray(data.errors)) {
                    showMessage('register-message', data.errors.join(', '), true);
                } else {
                    showMessage('register-message', data.message || 'Registration failed!', true);
                }
            }
        } catch (err) {
            toast.error('Registration failed!');
        }
    };
}

if (document.getElementById('forgot-password-form')) {
    const forgotPasswordForm = document.getElementById('forgot-password-form');
    forgotPasswordForm.onsubmit = async function(e) {
        e.preventDefault();
        const email = sanitizeInput(document.getElementById('forgot-email').value);
        
        if (!email) {
            showMessage('forgot-password-message', 'Email is required', true);
            return;
        }
        
        if (!isValidEmail(email)) {
            showMessage('forgot-password-message', 'Please enter a valid email address', true);
            return;
        }
        
        try {
            const res = await fetch(`${API_BASE_URL}/api/Auth/forgot-password`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email })
            });
            const data = await res.json();
            if (res.ok) {
                toast.success('Password reset link sent to your email!');
                forgotPasswordForm.reset();
            } else {
                if (data.errors && Array.isArray(data.errors)) {
                    showMessage('forgot-password-message', data.errors.join(', '), true);
                } else {
                    showMessage('forgot-password-message', data.message || 'Failed to send reset link!', true);
                }
            }
        } catch (err) {
            toast.error('Failed to send reset link!');
        }
    };
}

if (document.getElementById('reset-password-form')) {
    const resetPasswordForm = document.getElementById('reset-password-form');
    resetPasswordForm.onsubmit = async function(e) {
        e.preventDefault();
        const password = document.getElementById('reset-password').value;
        const confirmPassword = document.getElementById('reset-confirm-password').value;
        
        const errors = [];
        
        if (!password) {
            errors.push('Password is required');
        } else if (!isValidPassword(password)) {
            errors.push('Password must be at least 6 characters and contain at least one uppercase letter, one lowercase letter, and one number');
        }
        
        if (!confirmPassword) {
            errors.push('Confirm password is required');
        }
        
        if (password !== confirmPassword) {
            errors.push('Passwords do not match');
        }
        
        const urlParams = new URLSearchParams(window.location.search);
        const token = urlParams.get('token');
        
        if (!token) {
            errors.push('Invalid reset link');
        }
        
        if (errors.length > 0) {
            showMessage('reset-password-message', errors.join(', '), true);
            return;
        }
        
        try {
            const res = await fetch(`${API_BASE_URL}/api/Auth/reset-password`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ 
                    token, 
                    newPassword: password,
                    confirmPassword: confirmPassword 
                })
            });
            const data = await res.json();
            if (res.ok) {
                toast.success('Password reset successful! Redirecting to login...', 2000);
                setTimeout(() => {
                    window.location.href = '/auth/login.html';
                }, 2000);
            } else {
                if (data.errors && Array.isArray(data.errors)) {
                    showMessage('reset-password-message', data.errors.join(', '), true);
                } else {
                    showMessage('reset-password-message', data.message || 'Password reset failed!', true);
                }
            }
        } catch (err) {
            toast.error('Password reset failed!');
        }
    };
}

window.toggleUserStatus = async function(id, isActive) {
    try {
        await fetch(`${API_BASE_URL}/api/Auth/users/${id}/status`, {
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

if (document.getElementById('change-password-form')) {
    const changePasswordForm = document.getElementById('change-password-form');
    changePasswordForm.onsubmit = async function(e) {
        e.preventDefault();
        const currentPassword = document.getElementById('current-password').value;
        const newPassword = document.getElementById('new-password').value;
        const confirmPassword = document.getElementById('confirm-password').value;
        
        const errors = [];
        
        if (!currentPassword) {
            errors.push('Current password is required');
        } else if (currentPassword.length < 6) {
            errors.push('Current password must be at least 6 characters');
        }
        
        if (!newPassword) {
            errors.push('New password is required');
        } else if (!isValidPassword(newPassword)) {
            errors.push('New password must be at least 6 characters and contain at least one uppercase letter, one lowercase letter, and one number');
        }
        
        if (!confirmPassword) {
            errors.push('Confirm password is required');
        }
        
        if (newPassword !== confirmPassword) {
            errors.push('New passwords do not match');
        }
        
        if (errors.length > 0) {
            showMessage('change-password-message', errors.join(', '), true);
            return;
        }
        
        try {
            const res = await fetch(`${API_BASE_URL}/api/Auth/change-password`, {
                method: 'POST',
                headers: { 
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${authToken}`
                },
                body: JSON.stringify({ 
                    currentPassword,
                    newPassword,
                    confirmPassword 
                })
            });
            const data = await res.json();
            if (res.ok) {
                toast.success('Password changed successfully!');
                changePasswordForm.reset();
            } else {
                if (data.errors && Array.isArray(data.errors)) {
                    showMessage('change-password-message', data.errors.join(', '), true);
                } else {
                    showMessage('change-password-message', data.message || 'Password change failed!', true);
                }
            }
        } catch (err) {
            toast.error('Password change failed!');
        }
    };
}

const GOOGLE_CLIENT_ID = '157841978934-fmgq60lshk9iq65s7h37mc7ta78m8nu3.apps.googleusercontent.com';
const GOOGLE_REDIRECT_URI = 'http://localhost:8080/auth/login.html';
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
    if (code && path.endsWith('/auth/login.html')) {
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
                toast.success('Google login successful! Redirecting...');
                setTimeout(() => {
                    window.location.href = '/admin/dashboard.html';
                }, 1000);
                success = true;
            } else {
                const errorMsg = data.message || 'Google login failed!';
                if (msg) msg.textContent = errorMsg;
                toast.error(errorMsg);
            }
        } catch (err) {
            const errorMsg = 'Google login failed!';
            if (msg) msg.textContent = errorMsg;
            toast.error(errorMsg);
        }
        if (!success && msg) {
            msg.textContent = 'Google login failed!';
        }
    }
});

function handleVerifyEmailHashRoute() {
    if (window.location.hash.startsWith('#verify-email')) {
        document.querySelectorAll('.section').forEach(sec => sec.classList.remove('active'));
        var verifySection = document.getElementById('verify-email-section');
        if (verifySection) verifySection.classList.add('active');
        var title = document.getElementById('verify-email-title');
        var msg = document.getElementById('verify-email-message');
        title.textContent = 'Verifying your email...';
        msg.textContent = '';
        toast.info('Verifying your email...', 0);
        var hashParams = new URLSearchParams(window.location.hash.replace('#verify-email', ''));
        var token = hashParams.get('token');
        if (token) {
            fetch(`${API_BASE_URL}/api/Auth/verify-email?token=${encodeURIComponent(token)}`)
                .then(res => res.json().then(data => ({ ok: res.ok, data })))
                .then(({ ok, data }) => {
                    if (ok) {
                        title.textContent = 'Email Verified!';
                        msg.textContent = 'Your account has been successfully verified. You can now log in.';
                        toast.success('Email verified successfully! You can now log in.');
                    } else {
                        title.textContent = 'Verification Failed';
                        toast.error('Email verification failed!');
                        msg.textContent = data.message || 'Verification failed or the token has expired.';
                    }
                })
                .catch(() => {
                    title.textContent = 'Verification Failed';
                    msg.textContent = 'An error occurred during verification.';
                });
        } else {
            title.textContent = 'Verification Failed';
            msg.textContent = 'Invalid verification link.';
        }
    }
}
window.handleVerifyEmailHashRoute = handleVerifyEmailHashRoute;
window.addEventListener('hashchange', handleVerifyEmailHashRoute);
handleVerifyEmailHashRoute();

document.addEventListener('DOMContentLoaded', function() {
    const navUsers = document.getElementById('nav-users');
    const navSessions = document.getElementById('nav-sessions');
    const navSettings = document.getElementById('nav-settings');
    const usersSection = document.getElementById('users-section');
    const sessionsSection = document.getElementById('sessions-section');
    const settingsSection = document.getElementById('settings-section');
    if (navUsers && navSessions && navSettings && usersSection && sessionsSection && settingsSection) {
        navUsers.onclick = function() {
            navUsers.classList.add('active');
            navSessions.classList.remove('active');
            navSettings.classList.remove('active');
            usersSection.style.display = '';
            sessionsSection.style.display = 'none';
            settingsSection.style.display = 'none';
        };
        navSessions.onclick = function() {
            navUsers.classList.remove('active');
            navSessions.classList.add('active');
            navSettings.classList.remove('active');
            usersSection.style.display = 'none';
            sessionsSection.style.display = '';
            settingsSection.style.display = 'none';
        };
        navSettings.onclick = function() {
            navUsers.classList.remove('active');
            navSessions.classList.remove('active');
            navSettings.classList.add('active');
            usersSection.style.display = 'none';
            sessionsSection.style.display = 'none';
            settingsSection.style.display = '';
        };
    }
    
    setupLogoutHandlers();
});

(function() {
    const isAdminPage = window.location.pathname.startsWith('/admin/');
    const isLoginPage = window.location.pathname.endsWith('/auth/login.html');
    if (isAdminPage && !isAuthenticated()) {
        window.location.href = '/auth/login.html';
    }
    if (isLoginPage && isAuthenticated()) {
        window.location.href = '/admin/dashboard.html';
    }
})();

function setupLogoutHandlers() {
    const logoutBtn = document.getElementById('logout-btn');
    console.log('Setting up logout handlers, logout button found:', !!logoutBtn);
    if (logoutBtn && !logoutBtn.hasAttribute('data-logout-handler')) {
        console.log('Adding logout handler to button');
        logoutBtn.setAttribute('data-logout-handler', 'true');
        logoutBtn.addEventListener('click', async function(e) {
            console.log('Logout button clicked!');
            e.preventDefault();
            e.stopPropagation();
            logoutBtn.disabled = true;
            logoutBtn.textContent = 'Logging out...';
            try {
                console.log('Calling logout API...');
                await logoutUser();
                console.log('Logout API call completed successfully');
            } catch (error) {
                console.log('Logout API call failed, continuing with local logout:', error);
            }
            console.log('Clearing local data...');
            localStorage.removeItem('authToken');
            sessionStorage.clear();
            currentUser = null;
            authToken = null;
            if (window.toast) {
                toast.success('Logged out successfully!');
            }
            console.log('Redirecting to login page...');
            setTimeout(() => {
                window.location.href = '/auth/login.html';
            }, 500);
        });
        logoutBtn.onclick = logoutBtn.onclick || function(e) {
            console.log('Fallback logout handler triggered');
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
    window.location.href = '/admin/dashboard.html';
}
function goToUsers() {
    window.location.href = '/admin/users.html';
}
function goToSessions() {
    window.location.href = '/admin/sessions.html';
}
function goToSettings() {
    window.location.href = '/admin/settings.html';
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
        toast.error('Failed to load users: ' + (err.message || 'Unknown error'));
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
    pageUsers.forEach(user => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td>${sanitizeHtml(user.id)}</td>
            <td>${sanitizeHtml(user.username)}</td>
            <td>${sanitizeHtml(user.fullName || '-')}</td>
            <td>${sanitizeHtml(user.email)}</td>
            <td>${sanitizeHtml(user.phoneNumber || '-')}</td>
            <td>${user.status || 'Active'}</td>
            <td>${user.isVerified ? 'Yes' : 'No'}</td>
            <td>${user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleDateString() : 'Never'}</td>
            <td>${new Date(user.createdAt).toLocaleDateString()}</td>
            <td>
                <button class="btn btn-edit-user">Edit</button>
                <button class="btn btn-danger btn-delete-user">Delete</button>
            </td>
        `;
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
                toast.success('User deleted successfully!');
                btn.closest('tr').remove();
            } catch (err) {
                toast.error('Delete failed: ' + (err.message || 'Unknown error'));
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
        window.onclick = function(event) {
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
                toast.success('User updated successfully!');
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
                toast.error('Update failed: ' + (err.message || 'Unknown error'));
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
        toast.error('User not found');
        return;
    }

    document.getElementById('edit-user-id').value = user.id;
    document.getElementById('edit-username').value = user.username;
    document.getElementById('edit-email').value = user.email;
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
document.getElementById('close-edit-modal').onclick = closeEditModal;
document.getElementById('edit-user-modal').onclick = function(e) {
    if (e.target === this) closeEditModal();
}; 