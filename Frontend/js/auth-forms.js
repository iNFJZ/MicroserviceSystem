import { sanitizeInput, isValidEmail, isValidPassword, isValidUsername, showToast, parseJwt } from "./auth-utils.js";
import { apiRequest } from "./api.js";

const API_BASE_URL = "http://localhost:5050";

const GOOGLE_CLIENT_ID = "157841978934-fmgq60lshk9iq65s7h37mc7ta78m8nu3.apps.googleusercontent.com";
const GOOGLE_REDIRECT_URI = window.location.origin + "/login";

function getGoogleOAuthUrl() {
  const params = new URLSearchParams({
    client_id: GOOGLE_CLIENT_ID,
    redirect_uri: GOOGLE_REDIRECT_URI,
    response_type: "code",
    scope: "openid email profile",
    access_type: "offline",
    prompt: "consent"
  });
  return `https://accounts.google.com/o/oauth2/v2/auth?${params.toString()}`;
}

document.getElementById("google-login-btn")?.addEventListener("click", function() {
  window.location.href = getGoogleOAuthUrl();
});

window.addEventListener("DOMContentLoaded", async function() {
  const urlParams = new URLSearchParams(window.location.search);
  const code = urlParams.get("code");
  if (code) {
    try {
      const res = await fetch(`${API_BASE_URL}/api/Auth/login/google`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          code: code,
          redirectUri: GOOGLE_REDIRECT_URI
        })
      });
      const data = await res.json();
      if (res.ok && data.token) {
        localStorage.setItem("authToken", data.token);
        showToast(window.i18next.t("googleLoginSuccessfulRedirecting"), false);
        setTimeout(() => {
                          window.location.href = "/admin/";
        }, 1000);
      } else {
        const errorMessage = data.message || window.i18next.t("googleLoginFailed");
        if (errorMessage.includes("deleted") || errorMessage.includes("banned")) {
          showToast(window.i18next.t("yourAccountHasBeenDeactivated"), true);
        } else {
          showToast(window.i18next.t(errorMessage), true);
        }
      }
    } catch (err) {
      showToast(window.i18next.t("googleLoginFailedPleaseTryAgain"), true);
    }
  }
});

window.addEventListener("DOMContentLoaded", async function() {
  const emailElem = document.getElementById("reset-password-email");
  if (emailElem) {
    let email = "";
    const urlParams = new URLSearchParams(window.location.search);
    const token = urlParams.get("token");
    if (token) {
      try {
        const res = await fetch(`${API_BASE_URL}/api/Auth/validate-reset-token?token=${encodeURIComponent(token)}`);
        const data = await res.json();
        if (res.ok && data.success && data.email) {
          email = data.email;
        } else {
          showToast(data.message || window.i18next.t("invalidOrExpiredResetToken"), true);
        }
      } catch (err) {
        showToast(window.i18next.t("failedToValidateResetToken"), true);
      }
    }
    emailElem.textContent = email || "not available";
  }
});

if (document.getElementById("login-form")) {
    const loginForm = document.getElementById("login-form");
    loginForm.addEventListener("submit", async function(e) {
        e.preventDefault();
        
        const email = sanitizeInput(document.getElementById("login-email").value);
        const password = document.getElementById("login-password").value;
        
        const errors = [];
        
        if (!email) {
            errors.push(window.i18next.t("emailRequired"));
        } else if (!isValidEmail(email)) {
            errors.push(window.i18next.t("pleaseEnterValidEmailAddress"));
        }
        
        if (!password) {
            errors.push(window.i18next.t("passwordRequired"));
        } else if (password.length < 6) {
            errors.push(window.i18next.t("passwordMustBeAtLeast6Characters"));
        }
        
        if (errors.length > 0) {
            const errorMsgs = errors.map(e => typeof e === 'string' && e.startsWith('emailRequired') || e.startsWith('passwordRequired') ? window.i18next.t(e) : e);
            showToast(errorMsgs.join("\n"), true);
            return;
        }
        
        try {
            const res = await fetch(`${API_BASE_URL}/api/Auth/login`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ email, password })
            });
            const data = await res.json();
            
            if (res.ok && data.token) {
                localStorage.setItem("authToken", data.token);
                showToast(window.i18next.t("loginSuccessfulRedirecting"), false);
                setTimeout(() => {
                    window.location.href = "/admin/";
                }, 1000);
            } else {
                if (data.errors && Array.isArray(data.errors)) {
                    showToast(data.errors.map(e => window.i18next.t(e)).join(", "), true);
                } else {
                    const errorMessage = data.message || window.i18next.t("loginFailed");
                    if (errorMessage.includes("deleted") || errorMessage.includes("banned")) {
                        showToast(window.i18next.t("yourAccountHasBeenDeactivated"), true);
                    } else {
                        showToast(window.i18next.t(errorMessage), true);
                    }
                }
            }
        } catch (err) {
            console.error("Login error:", err);
            showToast(window.i18next.t("loginFailedPleaseTryAgain"), true);
        }
    });
}

if (document.getElementById("register-form")) {
    const registerForm = document.getElementById("register-form");
    registerForm.addEventListener("submit", async function(e) {
        e.preventDefault();
        
        const username = sanitizeInput(document.getElementById("register-username").value);
        const fullName = sanitizeInput(document.getElementById("register-fullname").value);
        const email = sanitizeInput(document.getElementById("register-email").value);
        const password = document.getElementById("register-password").value;
        const termsChecked = document.getElementById("terms-conditions")?.checked;
        
        const errors = [];
        
        if (!username) {
            errors.push(window.i18next.t("usernameRequired"));
        } else if (!isValidUsername(username)) {
            errors.push(window.i18next.t("usernameInvalid"));
        }
        
        if (!email) {
            errors.push(window.i18next.t("emailRequired"));
        } else if (!isValidEmail(email)) {
            errors.push(window.i18next.t("pleaseEnterValidEmailAddress"));
        }
        
        if (!password) {
            errors.push(window.i18next.t("passwordRequired"));
        } else if (!isValidPassword(password)) {
            errors.push(window.i18next.t("passwordInvalid"));
        }
        
        if (fullName && !/^[a-zA-ZÀ-ỹ\s]+$/.test(fullName)) {
            errors.push(window.i18next.t("fullNameInvalidCharacters"));
        }
        
        if (!termsChecked) {
            errors.push(window.i18next.t("mustAgreeToTerms"));
        }
        
        if (errors.length > 0) {
            showToast(errors.filter(Boolean).join(", "), true);
            return;
        }
        
        try {
            const res = await fetch(`${API_BASE_URL}/api/Auth/register`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ 
                    username, 
                    fullName: fullName || null, 
                    email, 
                    password 
                })
            });
            const data = await res.json();
            
            if (res.ok) {
                localStorage.setItem("pendingVerificationEmail", email);
                showToast(window.i18next.t("registrationSuccessfulCheckEmail"), false);
                setTimeout(() => {
                    window.location.href = "/auth/verify-email.html";
                }, 1000);
            } else {
                if (data.errors && Array.isArray(data.errors)) {
                    showToast(data.errors.map(e => window.i18next.t(e)).join(", "), true);
                } else {
                    showToast(window.i18next.t(data.message || "registrationFailed"), true);
                }
            }
        } catch (err) {
            console.error("Register error:", err);
            showToast(window.i18next.t("registrationFailedTryAgain"), true);
        }
    });
}

if (document.getElementById("forgot-password-form")) {
    const forgotPasswordForm = document.getElementById("forgot-password-form");
    forgotPasswordForm.addEventListener("submit", async function(e) {
        e.preventDefault();
        
        const email = sanitizeInput(document.getElementById("forgot-password-email").value);
        
        if (!email) {
            showToast(window.i18next.t("emailRequired"), true);
            return;
        }
        
        if (!isValidEmail(email)) {
            showToast(window.i18next.t("pleaseEnterValidEmailAddress"), true);
            return;
        }
        
        try {
            const res = await fetch(`${API_BASE_URL}/api/Auth/forgot-password`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ email })
            });
            const data = await res.json();
            
            if (res.ok) {
                localStorage.setItem("pendingResetEmail", email);
                showToast(window.i18next.t("resetEmailSentCheckEmail"), false);
                setTimeout(() => {
                    window.location.href = "/auth/login.html";
                }, 1500);
                forgotPasswordForm.reset();
            } else {
                showToast(window.i18next.t(data.message || "failedToSendResetEmail"), true);
            }
        } catch (err) {
            console.error("Forgot password error:", err);
            showToast(window.i18next.t("failedToSendResetEmailTryAgain"), true);
        }
    });
}

if (document.getElementById("reset-password-form")) {
    const resetPasswordForm = document.getElementById("reset-password-form");
    resetPasswordForm.addEventListener("submit", async function(e) {
        e.preventDefault();
        
        const password = document.getElementById("reset-password-password").value;
        const confirmPassword = document.getElementById("reset-password-confirm-password").value;
        
        if (!password) {
            showToast(window.i18next.t("passwordRequired"), true);
            return;
        }
        
        if (!isValidPassword(password)) {
            showToast(window.i18next.t("passwordInvalid"), true);
            return;
        }
        
        if (password !== confirmPassword) {
            showToast(window.i18next.t("passwordsDoNotMatch"), true);
            return;
        }
        
        const urlParams = new URLSearchParams(window.location.search);
        const token = urlParams.get("token");
        
        if (!token) {
            showToast(window.i18next.t("invalidResetToken"), true);
            return;
        }
        
        try {
            const res = await fetch(`${API_BASE_URL}/api/Auth/reset-password`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ 
                    token: token,
                    newPassword: password,
                    confirmPassword: confirmPassword
                })
            });
            const data = await res.json();
            
            if (res.ok) {
                showToast(window.i18next.t("passwordResetSuccessRedirectLogin"), false);
                localStorage.removeItem("pendingResetEmail");
                setTimeout(() => {
                    window.location.href = "/auth/login.html";
                }, 2000);
            } else {
                showToast(window.i18next.t(data.message || "passwordResetFailed"), true);
            }
        } catch (err) { 
            showToast(window.i18next.t("passwordResetFailedTryAgain"), true);
        }
    });
}

if (document.getElementById("change-password-form")) {
    const changePasswordForm = document.getElementById("change-password-form");
    changePasswordForm.addEventListener("submit", async function(e) {
        e.preventDefault();
        
        const currentPassword = document.getElementById("change-password-current").value;
        const newPassword = document.getElementById("change-password-new").value;
        const confirmPassword = document.getElementById("change-password-confirm").value;
        
        if (!currentPassword) {
            showToast(window.i18next.t("currentPasswordRequired"), true);
            return;
        }
        
        if (!newPassword) {
            showToast(window.i18next.t("newPasswordRequired"), true);
            return;
        }
        
        if (!isValidPassword(newPassword)) {
            showToast(window.i18next.t("newPasswordInvalid"), true);
            return;
        }
        
        if (newPassword !== confirmPassword) {
            showToast(window.i18next.t("newPasswordsDoNotMatch"), true);
            return;
        }
        
        const token = localStorage.getItem("authToken");
        if (!token) {
            showToast(window.i18next.t("mustBeLoggedInToChangePassword"), true);
            return;
        }
        
        try {
            const res = await fetch(`${API_BASE_URL}/api/Auth/change-password`, {
                method: "POST",
                headers: { 
                    "Content-Type": "application/json",
                    "Authorization": `Bearer ${token}`
                },
                body: JSON.stringify({ 
                    currentPassword,
                    newPassword 
                })
            });
            const data = await res.json();
            
            if (res.ok) {
                showToast(window.i18next.t("passwordChangedSuccessfully"), false);
                changePasswordForm.reset();
            } else {
                showToast(window.i18next.t(data.message || "passwordChangeFailed"), true);
            }
        } catch (err) {
            showToast(window.i18next.t("passwordChangeFailedTryAgain"), true);
        }
    });
}
