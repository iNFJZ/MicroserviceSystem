// Auth module: quản lý token, xác thực, logout
import { parseJwt } from './utils.js';

const TOKEN_KEY = 'authToken';

export function getToken() {
    return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token) {
    localStorage.setItem(TOKEN_KEY, token);
}

export function removeToken() {
    localStorage.removeItem(TOKEN_KEY);
}

export function isAuthenticated() {
    const token = getToken();
    if (!token) return false;
    const payload = parseJwt(token);
    if (!payload || !payload.exp) return false;
    // Kiểm tra hết hạn
    const now = Math.floor(Date.now() / 1000);
    return payload.exp > now;
}

export function getCurrentUser() {
    const token = getToken();
    if (!token) return null;
    return parseJwt(token);
}

export function logout() {
    removeToken();
    window.location.href = '/auth/login.html';
} 