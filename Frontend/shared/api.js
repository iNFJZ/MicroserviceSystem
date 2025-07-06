// API module: chuẩn hóa gọi API, tự động gắn token, xử lý lỗi
import { getToken, logout } from '/shared/auth.js';

const API_BASE_URL = 'http://localhost:5050';

async function apiRequest(path, options = {}) {
    const token = getToken();
    const headers = options.headers || {};
    if (token) headers['Authorization'] = 'Bearer ' + token;
    headers['Content-Type'] = 'application/json';
    const res = await fetch(API_BASE_URL + path, {
        ...options,
        headers
    });
    if (res.status === 401) {
        logout();
        throw new Error('Unauthorized');
    }
    let data;
    try {
        data = await res.json();
    } catch {
        data = null;
    }
    if (!res.ok) {
        throw new Error(data?.message || 'API error');
    }
    return data;
}

export async function fetchUsers() {
    return apiRequest('/api/Auth/users', { method: 'GET' });
}

export async function deleteUser(userId) {
    return apiRequest(`/api/Auth/users/${userId}`, { method: 'DELETE' });
}

export async function updateUser(userId, userData) {
    return apiRequest(`/api/Auth/users/${userId}`, {
        method: 'PATCH',
        body: JSON.stringify(userData)
    });
}

export async function logoutUser() {
    return apiRequest('/api/Auth/logout', { method: 'POST' });
}

export { apiRequest };
