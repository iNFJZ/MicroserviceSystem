import { sanitizeHtml, isAuthenticated, logout, sanitizeInput, isValidEmail } from "./auth-utils.js";
import { fetchUsers, fetchDeletedUsers, updateUser, deleteUser, logoutUser, restoreUser, statistics, getUserById, getUserByEmail, getUserByUsername } from "./api.js";

if (typeof toastr !== "undefined") {
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

const API_BASE_URL = "http://localhost:5050";

let currentUser = null;
let authToken = localStorage.getItem("authToken") || null;
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
        element.className = `form-message ${isError ? "error" : "success"}`;
    }
}

window.toggleUserStatus = async function(id, isActive) {
    try {
        await fetch(`${API_BASE_URL}/api/User/${id}/status`, {
            method: "PATCH",
            headers: {
                "Authorization": `Bearer ${authToken}`,
                "Content-Type": "application/json"
            },
            body: JSON.stringify({ isActive: !isActive })
        });
        renderUserTable();
    } catch {}
};

window.deleteUser = async function(id) {
    if (confirm(window.i18next.t("confirmDeleteUser"))) {
        try {
            let currentUserInfo = null;
            const token = localStorage.getItem("authToken");
            if (token) {
                try {
                    const payload = JSON.parse(atob(token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")));
                    currentUserInfo = {
                        id: payload.nameid || payload.sub,
                        email: payload.email,
                        username: payload.username
                    };
                } catch (e) {
                    console.error("Failed to parse JWT token:", e);
                }
            }
            
            const res = await deleteUser(id);
            toastr.success(window.i18next.t("userDeletedSuccessfully"));
            
            if (res && res.data && currentUserInfo && 
                (res.data.id == currentUserInfo.id || 
                 res.data.email === currentUserInfo.email || 
                 res.data.username === currentUserInfo.username)) {
                localStorage.removeItem("authToken");
                sessionStorage.clear();
                toastr.info(window.i18next.t("yourAccountHasBeenDeleted"));
                setTimeout(() => {
                    window.location.href = "/auth/login.html";
                }, 1000);
                return;
            }
            
            if (res && res.message && res.message.includes("deactivated successfully")) {
                toastr.info(window.i18next.t("deactivationEmailSent"));
            }
            renderUserTable();
        } catch (error) {
            toastr.error(window.i18next.t("failedToDeleteUser"));
        }
    }
};

const GOOGLE_CLIENT_ID = "157841978934-fmgq60lshk9iq65s7h37mc7ta78m8nu3.apps.googleusercontent.com";
const GOOGLE_REDIRECT_URI = "http://localhost:8080/";
const GOOGLE_SCOPE = "openid email profile";
const GOOGLE_AUTH_URL =
    "https://accounts.google.com/o/oauth2/v2/auth" +
    "?response_type=code" +
    `&client_id=${encodeURIComponent(GOOGLE_CLIENT_ID)}` +
    `&redirect_uri=${encodeURIComponent(GOOGLE_REDIRECT_URI)}` +
    `&scope=${encodeURIComponent(GOOGLE_SCOPE)}` +
    "&access_type=offline" +
    "&prompt=select_account";

window.loginWithGoogle = function() {
    window.location.href = GOOGLE_AUTH_URL;
};

window.addEventListener("DOMContentLoaded", async () => {
    const urlParams = new URLSearchParams(window.location.search);
    const code = urlParams.get("code");
    const path = window.location.pathname;
    if (code && path.endsWith("/login")) {
        const msg = document.getElementById("login-message");
        if (msg) msg.textContent = window.i18next.t("loggingInWithGoogle");
        let success = false;
        try {
            const requestBody = { code, redirectUri: GOOGLE_REDIRECT_URI };
            const res = await fetch(`${API_BASE_URL}/api/Auth/login/google`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(requestBody)
            });
            let data = {};
            try { data = await res.json(); } catch {}
            if (res.ok && data.token) {
                localStorage.setItem("authToken", data.token);
                toastr.success(window.i18next.t("googleLoginSuccessfulRedirecting"));
                setTimeout(() => {
                    window.location.href = "/admin/";
                }, 1000);
                success = true;
            } else {
                let errorMsg = data.message || window.i18next.t("googleLoginFailed");
                if (errorMsg.toLowerCase().includes("banned")) {
                    errorMsg = window.i18next.t("accountBanned");
                } else if (errorMsg.toLowerCase().includes("deleted")) {
                    errorMsg = window.i18next.t("accountHasBeenDeletedContactSupport");
                }
                if (msg) msg.textContent = errorMsg;
                toastr.error(window.i18next.t("googleLoginFailed"));
            }
        } catch (err) {
            let errorMsg = (err && err.message) ? err.message : window.i18next.t("googleLoginFailed");
            if (msg) msg.textContent = errorMsg;
            toastr.error(window.i18next.t("googleLoginFailed"));
        }
        if (!success && msg) {
            msg.textContent = window.i18next.t("googleLoginFailed");
        }
    }
});

document.addEventListener("DOMContentLoaded", function() {
    setupLogoutHandlers();
});

(function() {
    const isLoginPage = window.location.pathname.endsWith("/login") || window.location.pathname.endsWith("/auth/login.html");
    const token = localStorage.getItem("authToken");
    if (!isLoginPage && !token) {
        window.location.href = "/auth/login.html";
    }
})();

function setupLogoutHandlers() {
    const logoutBtn = document.getElementById("logout-btn");
    if (!logoutBtn) return;
    if (logoutBtn && !logoutBtn.hasAttribute("data-logout-handler")) {
        logoutBtn.setAttribute("data-logout-handler", "true");
        logoutBtn.addEventListener("click", async function(e) {
            e.preventDefault();
            e.stopPropagation();
            logoutBtn.disabled = true;
            logoutBtn.textContent = window.i18next.t("loggingOut");
            try {
                await logoutUser();
            } catch (error) {
            }
            localStorage.removeItem("authToken");
            sessionStorage.clear();
            currentUser = null;
            authToken = null;
            toastr.success(window.i18next.t("loggedOutSuccessfully"));
            setTimeout(() => {
                window.location.href = "/auth/login.html";
            }, 500);
        });
        logoutBtn.onclick = logoutBtn.onclick || function(e) {
            e.preventDefault();
            e.stopPropagation();
            localStorage.removeItem("authToken");
            sessionStorage.clear();
            currentUser = null;
            authToken = null;
            window.location.href = "/auth/login.html";
        };
    }
}
window.setupLogoutHandlers = setupLogoutHandlers;

document.addEventListener("DOMContentLoaded", function() {
    setupLogoutHandlers();
    
    const observer = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.type === "childList") {
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
    window.location.href = "/auth/login.html";
}
function goToDashboard() {
    window.location.href = "/admin/";
}

window.addEventListener("DOMContentLoaded", async function() {
    try {
        await loadActiveUsers();
        await loadDeletedUsers();
        await updateUserStats();
        setupTabSwitching();
        setupLogoutHandlers();
    } catch (error) {
        console.error("Error initializing app:", error);
        toastr.error(window.i18next.t("failedToInitializeApplication"));
    }
});

async function loadActiveUsers() {
    const userTableBody = document.querySelector("#user-table tbody");
    const searchInput = document.getElementById("user-search-input");
    const paginationContainer = document.getElementById("pagination-container");
    if (!userTableBody) return;
    
    let loadingRow = document.createElement("tr");
    loadingRow.innerHTML = `<td colspan="8" style="text-align:center;"><span class="loading-spinner"></span> ${window.i18next.t("loadingUsers")}</td>`;
    userTableBody.innerHTML = "";
    userTableBody.appendChild(loadingRow);
    
    try {
        const users = await fetchUsers();
        allUsersCache = users;
        filteredUsersCache = users;
        currentPage = 1;
        renderUserTableWithPagination(filteredUsersCache, currentPage);
    } catch (err) {
        userTableBody.innerHTML = `<tr><td colspan="8">${window.i18next.t("failedToLoadUsers")}</td></tr>`;
        toastr.error(window.i18next.t("failedToLoadUsers") + (err.message || window.i18next.t("unknownError")));
    }
    
    if (searchInput) {
        searchInput.addEventListener("input", function() {
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
            if (e.target.classList.contains("page-link")) {
                const page = parseInt(e.target.getAttribute("data-page"));
                if (!isNaN(page)) {
                    currentPage = page;
                    renderUserTableWithPagination(filteredUsersCache, currentPage);
                }
            }
        };
    }
}

async function loadDeletedUsers() {
    const deletedUserTableBody = document.querySelector("#deleted-user-table tbody");
    const deletedSearchInput = document.getElementById("deleted-user-search-input");
    const deletedPaginationContainer = document.getElementById("deleted-pagination-container");
    if (!deletedUserTableBody) return;
    
    let loadingRow = document.createElement("tr");
    loadingRow.innerHTML = `<td colspan="8" style="text-align:center;"><span class="loading-spinner"></span> ${window.i18next.t("loadingDeletedUsers")}</td>`;
    deletedUserTableBody.innerHTML = "";
    deletedUserTableBody.appendChild(loadingRow);
    
    try {
        const deletedUsers = await fetchDeletedUsers();
        allDeletedUsersCache = deletedUsers;
        filteredDeletedUsersCache = deletedUsers;
        currentDeletedPage = 1;
        renderDeletedUserTableWithPagination(filteredDeletedUsersCache, currentDeletedPage);
    } catch (err) {
        deletedUserTableBody.innerHTML = `<tr><td colspan="8">${window.i18next.t("failedToLoadDeletedUsers")}</td></tr>`;
        toastr.error(window.i18next.t("failedToLoadDeletedUsers") + (err.message || window.i18next.t("unknownError")));
    }
    
    if (deletedSearchInput) {
        deletedSearchInput.addEventListener("input", function() {
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
            if (e.target.classList.contains("page-link")) {
                const page = parseInt(e.target.getAttribute("data-page"));
                if (!isNaN(page)) {
                    currentDeletedPage = page;
                    renderDeletedUserTableWithPagination(filteredDeletedUsersCache, currentDeletedPage);
                }
            }
        };
    }
}

function setupTabSwitching() {
    const activeTab = document.getElementById("active-users-tab");
    const deletedTab = document.getElementById("deleted-users-tab");
    
    if (activeTab) {
        activeTab.addEventListener("click", function() {
            loadActiveUsers();
        });
    }
    
    if (deletedTab) {
        deletedTab.addEventListener("click", function() {
            loadDeletedUsers();
        });
    }
}

function renderUserTableWithPagination(users, page) {
    const userTableBody = document.querySelector("#user-table tbody");
    const paginationContainer = document.getElementById("pagination-container");
    if (!userTableBody) return;
    if (!users || users.length === 0) {
        userTableBody.innerHTML = `<tr><td colspan="8">${window.i18next.t("noUsersFound")}</td></tr>`;
        if (paginationContainer) paginationContainer.innerHTML = "";
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
            <td>${sanitizeHtml(user.fullName || window.i18next.t("notAvailable"))}</td>
            <td>${sanitizeHtml(user.email)}</td>
            <td>${sanitizeHtml(user.phoneNumber || window.i18next.t("notAvailable"))}</td>
            <td>${
                Number(user.status) === 1
                    ? `<span class="badge bg-success">${window.i18next.t("active")}</span>`
                    : Number(user.status) === 2
                    ? `<span class="badge bg-secondary">${window.i18next.t("inactive")}</span>`
                    : Number(user.status) === 3
                    ? `<span class="badge bg-warning">${window.i18next.t("suspended")}</span>`
                    : Number(user.status) === 4
                    ? `<span class="badge bg-danger">${window.i18next.t("banned")}</span>`
                    : `<span class="badge bg-secondary">${window.i18next.t("unknown")}</span>`
            }</td>
            <td>${user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString("en-GB", { year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit", second: "2-digit" }) : window.i18next.t("never")}</td>
            <td>
                <button class="btn btn-sm btn-primary btn-edit-user" style="min-width:80px" data-user-idx="${idx}">${window.i18next.t("edit")}</button>
                <button class="btn btn-sm btn-danger btn-delete-user" style="min-width:80px" data-user-idx="${idx}">${window.i18next.t("delete")}</button>
            </td>
        </tr>
    `).join("");
    userTableBody.querySelectorAll("tr.user-row").forEach((row, idx) => {
        row.addEventListener("click", function(e) {
            if (e.target.closest("button")) return;
            showUserDetailModal(pageUsers[idx]);
        });
    });
    document.querySelectorAll(".btn-edit-user").forEach((btn, idx) => {
        btn.onclick = function(e) {
            e.preventDefault();
            const user = pageUsers[idx];
            editUser(user.id);
        };
    });
    document.querySelectorAll(".btn-delete-user").forEach((btn, idx) => {
        btn.onclick = async function(e) {
            e.preventDefault();
            const user = pageUsers[idx];
            if (!confirm(window.i18next.t("confirmDeleteUser"))) return;
            btn.disabled = true;
            btn.textContent = window.i18next.t("deleting");
            
            let currentUserInfo = null;
            const token = localStorage.getItem("authToken");
            if (token) {
                try {
                    const payload = JSON.parse(atob(token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")));
                    currentUserInfo = {
                        id: payload.nameid || payload.sub,
                        email: payload.email,
                        username: payload.username
                    };
                } catch (e) {
                    console.error("Failed to parse JWT token:", e);
                }
            }
            
            try {
                const res = await deleteUser(user.id);
                toastr.success(window.i18next.t("userDeletedSuccessfully"));
                
                if (res && res.data && currentUserInfo && 
                    (res.data.id == currentUserInfo.id || 
                     res.data.email === currentUserInfo.email || 
                     res.data.username === currentUserInfo.username)) {
                    localStorage.removeItem("authToken");
                    sessionStorage.clear();
                    toastr.info(window.i18next.t("yourAccountHasBeenDeleted"));
                    setTimeout(() => {
                        window.location.href = "/auth/login.html";
                    }, 1000);
                    return;
                }
                
                if (res && res.message && res.message.includes("deactivated successfully")) {
                    toastr.info(window.i18next.t("deactivationEmailSent"));
                }
                allUsersCache = [];
                filteredUsersCache = [];
                allDeletedUsersCache = [];
                filteredDeletedUsersCache = [];
                currentPage = 1;
                const searchInput = document.getElementById("user-search-input");
                if (searchInput) searchInput.value = "";
                await loadActiveUsers();
                await loadDeletedUsers();
                await updateUserStats();
            } catch (err) {
                toastr.error(window.i18next.t("deleteFailed"));
                btn.disabled = false;
                btn.textContent = window.i18next.t("delete");
            }
        };
    });
    if (paginationContainer) {
        let html = "";
        if (totalPages > 1) {
            html += `<button class="page-link" data-page="${page - 1}" ${page === 1 ? "disabled" : ""}>${window.i18next.t("previous")}</button>`;
            for (let i = 1; i <= totalPages; i++) {
                html += `<button class="page-link${i === page ? " active" : ""}" data-page="${i}">${i}</button>`;
            }
            html += `<button class="page-link" data-page="${page + 1}" ${page === totalPages ? "disabled" : ""}>${window.i18next.t("next")}</button>`;
            html += `<span class="pagination-info"> ${window.i18next.t("page")} ${page} ${window.i18next.t("of")} ${totalPages} ${window.i18next.t("totalUsers")}: ${total}</span>`;
        }
        paginationContainer.innerHTML = html;
    }
}

function renderDeletedUserTableWithPagination(users, page) {
    const userTableBody = document.querySelector("#deleted-user-table tbody");
    const paginationContainer = document.getElementById("deleted-pagination-container");
    if (!userTableBody) return;
    if (!users || users.length === 0) {
        userTableBody.innerHTML = `<tr><td colspan="8">${window.i18next.t("noDeletedUsersFound")}</td></tr>`;
        if (paginationContainer) paginationContainer.innerHTML = "";
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
            <td>${sanitizeHtml(user.fullName || window.i18next.t("notAvailable"))}</td>
            <td>${sanitizeHtml(user.email)}</td>
            <td>${sanitizeHtml(user.phoneNumber || window.i18next.t("notAvailable"))}</td>
            <td><span class="badge bg-danger">${window.i18next.t("deleted")}</span></td>
            <td>${user.deletedAt ? new Date(user.deletedAt).toLocaleString("en-GB", { year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit" }) : window.i18next.t("never")}</td>
            <td><button class="btn btn-sm btn-success btn-restore-user">${window.i18next.t("restore")}</button></td>
        </tr>
    `).join("");
    userTableBody.querySelectorAll("tr.deleted-user-row").forEach((row, idx) => {
        row.addEventListener("click", function(e) {
            if (e.target.closest("button")) return;
            showUserDetailModal(pageUsers[idx]);
        });
    });
    document.querySelectorAll(".btn-restore-user").forEach((btn, idx) => {
        btn.onclick = async function(e) {
            e.preventDefault();
            const user = pageUsers[idx];
            if (confirm(`${window.i18next.t("confirmRestoreUser")} "${user.username}"?`)) {
                btn.disabled = true;
                btn.textContent = window.i18next.t("restoring");
                try {
                    await restoreUser(user.id);
                    toastr.success(window.i18next.t("userRestoredSuccessfully"));
                    allUsersCache = [];
                    filteredUsersCache = [];
                    allDeletedUsersCache = [];
                    filteredDeletedUsersCache = [];
                    currentPage = 1;
                    const searchInput = document.getElementById("user-search-input");
                    if (searchInput) searchInput.value = "";
                    const deletedSearchInput = document.getElementById("deleted-user-search-input");
                    if (deletedSearchInput) deletedSearchInput.value = "";
                    await loadActiveUsers();
                    await loadDeletedUsers();
                    await updateUserStats();
                } catch (err) {
                    toastr.error(window.i18next.t("restoreFailed"));
                    btn.disabled = false;
                    btn.textContent = window.i18next.t("restore");
                }
            }
        };
    });
    if (paginationContainer) {
        let html = "";
        if (totalPages > 1) {
            html += `<button class="page-link" data-page="${page - 1}" ${page === 1 ? "disabled" : ""}>${window.i18next.t("previous")}</button>`;
            for (let i = 1; i <= totalPages; i++) {
                html += `<button class="page-link${i === page ? " active" : ""}" data-page="${i}">${i}</button>`;
            }
            html += `<button class="page-link" data-page="${page + 1}" ${page === totalPages ? "disabled" : ""}>${window.i18next.t("next")}</button>`;
            html += `<span class="pagination-info"> ${window.i18next.t("page")} ${page} ${window.i18next.t("of")} ${totalPages} ${window.i18next.t("totalDeletedUsers")}: ${total}</span>`;
        }
        paginationContainer.innerHTML = html;
    }
}

window.addEventListener("DOMContentLoaded", function() {
    const modal = document.getElementById("edit-user-modal");
    const closeBtn = document.getElementById("close-edit-modal");
    const closeBtnAlt = document.getElementById("close-edit-modal-btn");
    
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
        
        modal.addEventListener("click", function(event) {
            if (event.target === modal) {
                closeEditModal();
            }
        });
        
        const modalDialog = modal.querySelector(".modal-dialog");
        if (modalDialog) {
            modalDialog.addEventListener("click", function(event) {
                event.stopPropagation();
            });
            
            const form = modalDialog.querySelector("form");
            if (form) {
                form.addEventListener("click", function(event) {
                    event.stopPropagation();
                });
            }
            
            const modalContent = modalDialog.querySelector(".modal-content");
            if (modalContent) {
                modalContent.addEventListener("click", function(event) {
                    event.stopPropagation();
                });
            }
        }
        
        document.addEventListener("keydown", function(event) {
            if (event.key === "Escape" && modal.classList.contains("show")) {
                closeEditModal();
            }
        });
    }
    
    const editForm = document.getElementById("edit-user-form");
    if (editForm) {
        editForm.onsubmit = async function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            const userId = document.getElementById("edit-user-id").value;
            const username = document.getElementById("edit-username").value.trim();
            const fullName = sanitizeInput(document.getElementById("edit-fullName").value.trim());
            const email = sanitizeInput(document.getElementById("edit-email").value.trim());
            const phone = document.getElementById("edit-phone").value.trim();
            const dob = document.getElementById("edit-dob").value.trim();
            const address = sanitizeInput(document.getElementById("edit-address").value.trim());
            const bio = sanitizeInput(document.getElementById("edit-bio").value.trim());
            const status = document.getElementById("edit-status").value.trim();
            const isVerified = false;

            const errors = [];
            if (!fullName) {
                errors.push(window.i18next.t("fullNameRequired"));
            } else if (!/^[a-zA-ZÀ-ỹ\s]+$/.test(fullName)) {
                errors.push(window.i18next.t("fullNameInvalidCharacters"));
            }
            if (phone && !/^[0-9]{10,11}$/.test(phone)) {
                errors.push(window.i18next.t("phoneNumberInvalidFormat"));
            }
            if (address && address.length > 200) {
                errors.push(window.i18next.t("addressTooLong"));
            }
            if (bio && bio.length > 500) {
                errors.push(window.i18next.t("bioTooLong"));
            }
            if (!status || !["1","2","3","4"].includes(status)) {
                errors.push(window.i18next.t("statusRequired"));
            }
            if (errors.length > 0) {
                toastr.error(errors.filter(Boolean).join("<br>"));
                return;
            }
            
            const saveBtn = editForm.querySelector("button[type=\"submit\"]");
            saveBtn.disabled = true;
            saveBtn.textContent = window.i18next.t("saving");
            
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
                toastr.success(window.i18next.t("userUpdatedSuccessfully"));
                closeEditModal();
                
                const oldUser = allUsersCache.find(u => u.id === userId);
                if (oldUser && (oldUser.isVerified !== isVerified || oldUser.status !== parseInt(status))) {
                    allUsersCache = [];
                    filteredUsersCache = [];
                    allDeletedUsersCache = [];
                    filteredDeletedUsersCache = [];
                    const searchInput = document.getElementById("user-search-input");
                    if (searchInput) searchInput.value = "";
                    await loadActiveUsers();
                } else {
                    const idx = allUsersCache.findIndex(u => u.id === userId);
                    if (idx !== -1) {
                        allUsersCache[idx].fullName = fullName;
                        allUsersCache[idx].phoneNumber = phone;
                        allUsersCache[idx].dateOfBirth = dob ? dob + "T00:00:00" : null;
                        allUsersCache[idx].address = address;
                        allUsersCache[idx].bio = bio;
                        allUsersCache[idx].status = status ? parseInt(status) : 1;
                        allUsersCache[idx].isVerified = isVerified;
                        renderUserTableWithPagination(filteredUsersCache, currentPage);
                    }
                }
                
                await updateUserStats();
            } catch (err) {
                let msg = window.i18next.t("updateFailed");
                if (err && err.response && err.response.data && err.response.data.message) {
                    msg += err.response.data.message;
                } else if (err && err.message) {
                    msg += err.message;
                } else {
                    msg += window.i18next.t("unknownError");
                }
                toastr.error(msg);
            } finally {
                saveBtn.disabled = false;
                saveBtn.textContent = window.i18next.t("saveChanges");
            }
        };
    }
});

window.addEventListener("DOMContentLoaded", async function() {
    const userStatsContainer = document.getElementById("user-stats-container");
    if (userStatsContainer) {
        try {
            const users = await fetchUsers();
            const total = users.length;
            const verified = users.filter(u => u.isVerified).length;
            const unverified = total - verified;
            userStatsContainer.innerHTML = `
                <div class="stat-item"><span class="stat-label">${window.i18next.t("totalUsers")}:</span> <span class="stat-value">${total}</span></div>
                <div class="stat-item"><span class="stat-label">${window.i18next.t("verified")}:</span> <span class="stat-value">${verified}</span></div>
                <div class="stat-item"><span class="stat-label">${window.i18next.t("unverified")}:</span> <span class="stat-value">${unverified}</span></div>
            `;
        } catch (err) {
            userStatsContainer.innerHTML = `<span class="stat-error">${window.i18next.t("failedToLoadUserStats")}</span>`;
        }
    }
});

function preventBodyScroll() {
    document.body.style.overflow = "hidden";
    document.body.style.paddingRight = "0px";
}

function restoreBodyScroll() {
    document.body.style.overflow = "";
    document.body.style.paddingRight = "";
}

let activeModal = null;

function setActiveModal(modal) {
    if (activeModal && activeModal !== modal) {
        activeModal.classList.remove("show");
        setTimeout(() => {
            activeModal.style.display = "none";
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
        toastr.error(window.i18next.t("userNotFound"));
        return;
    }

    document.getElementById("edit-user-id").value = user.id;
    document.getElementById("edit-username").value = user.username;
    document.getElementById("edit-email").value = user.email || "";
    document.getElementById("edit-fullName").value = user.fullName || "";
    document.getElementById("edit-phone").value = user.phoneNumber || "";
    document.getElementById("edit-dob").value = user.dateOfBirth ? user.dateOfBirth.split("T")[0] : "";
    document.getElementById("edit-address").value = user.address || "";
    document.getElementById("edit-bio").value = user.bio || "";
    let statusVal = [1,2,3,4].includes(Number(user.status)) ? String(user.status) : "2";
    document.getElementById("edit-status").value = statusVal;

    const modal = document.getElementById("edit-user-modal");
    if (!modal) {
        toastr.error(window.i18next.t("modalNotFound"));
        return;
    }

    setActiveModal(modal);
    modal.style.display = "block";
    
    setTimeout(() => {
        modal.classList.add("show");
        preventBodyScroll();
        
        const firstInput = modal.querySelector("input:not([disabled])");
        if (firstInput) {
            firstInput.focus();
        }
    }, 50);
}

function closeEditModal() {
    const modal = document.getElementById("edit-user-modal");
    if (!modal) return;
    
    modal.classList.remove("show");
    
    restoreBodyScroll();
    
    clearActiveModal();
    
    setTimeout(() => {
        modal.style.display = "none";
        
        const form = document.getElementById("edit-user-form");
        if (form) {
            form.reset();
        }
    }, 300);
}

function showUserDetailModal(user) {
    const modal = document.getElementById("user-detail-modal");
    const content = document.getElementById("user-detail-content");
    let deletedAtRow = "";
    if (user.deletedAt) {
        deletedAtRow = `<div><b>${window.i18next.t("deletedAt")}:</b> ${new Date(user.deletedAt).toLocaleString("en-GB", { year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit" })}</div>`;
    }
    content.innerHTML = `
      <div><b>${window.i18next.t("id")}:</b> ${sanitizeHtml(user.id)}</div>
      <div><b>${window.i18next.t("username")}:</b> ${sanitizeHtml(user.username)}</div>
      <div><b>${window.i18next.t("fullName")}:</b> ${sanitizeHtml(user.fullName || window.i18next.t("notAvailable"))}</div>
      <div><b>${window.i18next.t("email")}:</b> ${sanitizeHtml(user.email)}</div>
      <div><b>${window.i18next.t("phone")}:</b> ${sanitizeHtml(user.phoneNumber || window.i18next.t("notAvailable"))}</div>
      <div><b>${window.i18next.t("status")}:</b> ${(() => { switch(user.status){case 1: return window.i18next.t("active");case 2: return window.i18next.t("inactive");case 3: return window.i18next.t("suspended");case 4: return window.i18next.t("banned");default: return sanitizeHtml(user.status || "");}})()}</div>
      <div><b>${window.i18next.t("lastLogin")}:</b> ${user.lastLoginAt ? new Date(user.lastLoginAt).toLocaleString("en-GB", { year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit", second: "2-digit" }) : window.i18next.t("never")}</div>
      ${deletedAtRow}
      <div><b>${window.i18next.t("address")}:</b> ${sanitizeHtml(user.address || window.i18next.t("notAvailable"))}</div>
      <div><b>${window.i18next.t("bio")}:</b> ${sanitizeHtml(user.bio || window.i18next.t("notAvailable"))}</div>
      <div><b>${window.i18next.t("dateOfBirth")}:</b> ${user.dateOfBirth ? new Date(user.dateOfBirth).toLocaleDateString("en-GB") : window.i18next.t("notAvailable")}</div>
      <div><b>${window.i18next.t("provider")}:</b> ${sanitizeHtml(user.loginProvider || window.i18next.t("notAvailable"))}</div>
      <div><b>${window.i18next.t("profilePicture")}:</b> ${user.profilePicture ? `<img src='${user.profilePicture}' alt='avatar' style='max-width:60px;max-height:60px;border-radius:50%;'/>` : window.i18next.t("notAvailable")}</div>
    `;
    setActiveModal(modal);
    modal.style.display = "block";
    modal.classList.add("show");
    preventBodyScroll();
}

function closeUserDetailModal() {
    const modal = document.getElementById("user-detail-modal");
    modal.classList.remove("show");
    restoreBodyScroll();
    clearActiveModal();
    setTimeout(() => {
        modal.style.display = "none";
    }, 300);
}

window.addEventListener("DOMContentLoaded", function() {
    const userDetailModal = document.getElementById("user-detail-modal");
    const closeUserDetailBtn = document.getElementById("close-user-detail-modal");
    
    if (userDetailModal && closeUserDetailBtn) {
        closeUserDetailBtn.onclick = function(e) {
            e.preventDefault();
            e.stopPropagation();
            closeUserDetailModal();
        };
        
        userDetailModal.addEventListener("click", function(event) {
            if (event.target === userDetailModal) {
                closeUserDetailModal();
            }
        });
        
        const modalDialog = userDetailModal.querySelector(".modal-dialog");
        if (modalDialog) {
            modalDialog.addEventListener("click", function(event) {
                event.stopPropagation();
            });
        }
        
        document.addEventListener("keydown", function(event) {
            if (event.key === "Escape" && userDetailModal.classList.contains("show")) {
                closeUserDetailModal();
            }
        });
    }
});

window.addEventListener("DOMContentLoaded", function() {
    const usernameElem = document.getElementById("current-username");
    if (usernameElem) {
        try {
            const token = localStorage.getItem("authToken");
            if (token) {
                const payload = JSON.parse(atob(token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")));
                if (payload && payload.username) {
                    usernameElem.textContent = payload.username;
                } else if (payload && payload.fullName) {
                    usernameElem.textContent = payload.fullName;
                } else if (payload && payload.email) {
                    usernameElem.textContent = payload.email;
                } else {
                    usernameElem.textContent = window.i18next.t("user");
                }
            } else {
                usernameElem.textContent = window.i18next.t("user");
            }
        } catch {
            usernameElem.textContent = window.i18next.t("user");
        }
    }
});

async function updateUserStats() {
    const userStatsContainer = document.getElementById("user-stats-container");
    if (userStatsContainer) {
        try {
            const users = await fetchUsers();
            const total = users.length;
            const verified = users.filter(u => u.isVerified).length;
            const unverified = total - verified;
            userStatsContainer.innerHTML = `
                <div class="stat-item"><span class="stat-label">${window.i18next.t("totalUsers")}:</span> <span class="stat-value">${total}</span></div>
                <div class="stat-item"><span class="stat-label">${window.i18next.t("verified")}:</span> <span class="stat-value">${verified}</span></div>
                <div class="stat-item"><span class="stat-label">${window.i18next.t("unverified")}:</span> <span class="stat-value">${unverified}</span></div>
            `;
        } catch (err) {
            userStatsContainer.innerHTML = `<span class="stat-error">${window.i18next.t("failedToLoadUserStats")}</span>`;
        }
    }
} 