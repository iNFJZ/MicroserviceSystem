import { getToken, logout } from "./auth-utils.js";

const API_BASE_URL = "http://localhost:5050";

export async function apiRequest(path, options = {}) {
    const token = getToken();
    const headers = options.headers || {};
    if (token) headers["Authorization"] = "Bearer " + token;
    headers["Content-Type"] = "application/json";
    headers["Accept-Language"] = window.i18next?.language || localStorage.getItem("i18nextLng") || "en";
    
    const res = await fetch(API_BASE_URL + path, {
        ...options,
        headers
    });
    
    let data;
    try {
        data = await res.json();
    } catch {
        data = null;
    }
    
    if (!res.ok) {
        // Use error handler to display localized error messages
        if (window.errorHandler && data) {
            window.errorHandler.handleApiError(data);
        } else {
            // Fallback to default error handling
            const message = data?.message || `HTTP ${res.status}: ${res.statusText}`;
            if (typeof toastr !== 'undefined') {
                toastr.error(message);
            } else {
                alert(message);
            }
        }
        
        if (res.status === 401) {
            logout();
        }
        
        throw new Error(data?.message || "API error");
    }
    
    return data;
}



export async function fetchUsers() {
    const res = await apiRequest("/api/User", { method: "GET" });
    return res.data;
}

export async function fetchDeletedUsers() {
    const res = await apiRequest("/api/User/deleted", { method: "GET" });
    return res.data;
}

export async function getUserById(userId) {
    return apiRequest(`/api/User/${userId}`, { method: "GET" });
}

export async function updateUser(userId, userData) {
    return apiRequest(`/api/User/${userId}`, {
        method: "PUT",
        body: JSON.stringify(userData)
    });
}

export async function deleteUser(userId) {
    return apiRequest(`/api/User/${userId}`, { method: "DELETE" });
}

export async function getUserByEmail(email) {
    return apiRequest(`/api/User/email/${email}`, { method: "GET" });
}

export async function getUserByUsername(username) {
    return apiRequest(`/api/User/username/${username}`, { method: "GET" });
}

export async function restoreUser(userId) {
    return apiRequest(`/api/User/${userId}/restore`, {
        method: "PATCH",
    });
}

export async function statistics() {
    return apiRequest("/api/User/statistics", { method: "GET" });
}

export async function logoutUser() {
    return apiRequest("/api/Auth/logout", { method: "POST" });
}
