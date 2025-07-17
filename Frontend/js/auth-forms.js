import {
  sanitizeInput,
  isValidEmail,
  isValidPassword,
  isValidUsername,
} from "./auth-utils.js";
import { apiRequest } from "./api.js";

const API_BASE_URL = "http://localhost:5050/api";

const GOOGLE_CLIENT_ID =
  "157841978934-fmgq60lshk9iq65s7h37mc7ta78m8nu3.apps.googleusercontent.com";
const GOOGLE_REDIRECT_URI = window.location.origin + "/login";

function getGoogleOAuthUrl() {
  const params = new URLSearchParams({
    client_id: GOOGLE_CLIENT_ID,
    redirect_uri: GOOGLE_REDIRECT_URI,
    response_type: "code",
    scope: "openid email profile",
    access_type: "offline",
    prompt: "consent",
  });
  return `https://accounts.google.com/o/oauth2/v2/auth?${params.toString()}`;
}

document
  .getElementById("google-login-btn")
  ?.addEventListener("click", function () {
    window.location.href = getGoogleOAuthUrl();
  });

window.addEventListener("DOMContentLoaded", async function () {
  const urlParams = new URLSearchParams(window.location.search);
  const code = urlParams.get("code");
  if (code) {
    try {
      const language = getCurrentLanguage();
      const res = await fetch(`${API_BASE_URL}/Auth/login/google`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          code: code,
          redirectUri: GOOGLE_REDIRECT_URI,
          language,
        }),
      });
      const data = await res.json();
      const errorCode = data.errorCode || data.ErrorCode;
      if (res.ok && data.token) {
        localStorage.setItem("authToken", data.token);
        if (data.language) {
          window.i18next.changeLanguage(data.language);
        }
        window.showToastr(
          window.i18next.t("googleLoginSuccessfulRedirecting"),
          "success",
        );
        setTimeout(() => {
          window.location.href = "/admin/";
        }, 1000);
      } else {
        const errorMessage =
          data.message || window.i18next.t("googleLoginFailed");
        if (
          errorCode === "ACCOUNT_DELETED" ||
          errorMessage.includes("deleted")
        ) {
          window.showToastr(
            window.i18next.t("accountHasBeenDeletedContactSupport"),
            "error",
          );
        } else if (
          errorCode === "ACCOUNT_BANNED" ||
          errorMessage.includes("banned")
        ) {
          window.showToastr(
            window.i18next.t("yourAccountHasBeenDeactivated"),
            "error",
          );
        } else {
          window.showToastr(window.i18next.t(errorMessage), "error");
        }
      }
    } catch (err) {
      window.showToastr(
        window.i18next.t("googleLoginFailedPleaseTryAgain"),
        "error",
      );
    }
  }
});

window.addEventListener("DOMContentLoaded", async function () {
  const emailElem = document.getElementById("reset-password-email");
  const descElem = document.querySelector(
    '[data-i18n="auth.reset-password.description"]',
  );
  if (emailElem) {
    let email = "";
    const urlParams = new URLSearchParams(window.location.search);
    const token = urlParams.get("token");
    if (token) {
      try {
        const res = await fetch(
          `${API_BASE_URL}/Auth/validate-reset-token?token=${encodeURIComponent(token)}`,
        );
        const data = await res.json();
        if (res.ok && data.success && data.email) {
          email = data.email;
        } else {
          window.showToastr(
            data.message || window.i18next.t("invalidOrExpiredResetToken"),
            "error",
          );
        }
      } catch (err) {
        window.showToastr(
          window.i18next.t("failedToValidateResetToken"),
          "error",
        );
      }
    }
    emailElem.textContent = email || "not available";
    if (descElem) {
      descElem.textContent = window.i18next
        .t("auth.reset-password.description")
        .replace("{email}", email || "...");
    }
  }
});

window.addEventListener("DOMContentLoaded", function () {
  const userEmailElem = document.getElementById("userEmail");
  if (userEmailElem) {
    userEmailElem.textContent = window.i18next
      ? window.i18next.t("notAvailable")
      : "not available";
  }
});

if (document.getElementById("login-form")) {
  const loginForm = document.getElementById("login-form");
  loginForm.addEventListener("submit", async function (e) {
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
      const errorMsgs = errors.map((e) =>
        (typeof e === "string" && e.startsWith("emailRequired")) ||
        e.startsWith("passwordRequired")
          ? window.i18next.t(e)
          : e,
      );
      window.showToastr(errorMsgs.join("\n"), "error");
      return;
    }

    try {
      const language = getCurrentLanguage();
      const res = await fetch(`${API_BASE_URL}/Auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password, language }),
      });
      const data = await res.json();

      if (res.ok && data.token) {
        localStorage.setItem("authToken", data.token);
        if (data.language) {
          window.i18next.changeLanguage(data.language);
        }
        window.showToastr(
          window.i18next.t("loginSuccessfulRedirecting"),
          "success",
        );
        setTimeout(() => {
          window.location.href = "/admin/";
        }, 1000);
      } else {
        // Use error handler for localized error messages
        if (window.errorHandler && data) {
          window.errorHandler.handleApiError(data);
        } else {
          // Fallback to old error handling
          if (data.errors && Array.isArray(data.errors)) {
            window.showToastr(
              data.errors.map((e) => window.i18next.t(e)).join(", "),
              "error",
            );
          } else {
            const errorMessage =
              data.message || window.i18next.t("loginFailed");
            if (errorMessage.includes("deleted")) {
              window.showToastr(
                window.i18next.t("accountHasBeenDeletedContactSupport"),
                "error",
              );
            } else if (errorMessage.includes("banned")) {
              window.showToastr(
                window.i18next.t("yourAccountHasBeenDeactivated"),
                "error",
              );
            } else if (
              errorMessage.includes("Invalid email or password") ||
              errorMessage.includes("Email hoặc mật khẩu không đúng") ||
              errorMessage.includes(
                "メールアドレスまたはパスワードが正しくありません",
              )
            ) {
              window.showToastr(
                window.i18next.t("invalidCredentials"),
                "error",
              );
            } else {
              window.showToastr(window.i18next.t(errorMessage), "error");
            }
          }
        }
      }
    } catch (err) {
      console.error("Login error:", err);
      window.showToastr(window.i18next.t("loginFailedPleaseTryAgain"), "error");
    }
  });
}

if (document.getElementById("register-form")) {
  const registerForm = document.getElementById("register-form");
  registerForm.addEventListener("submit", async function (e) {
    e.preventDefault();

    const username = sanitizeInput(
      document.getElementById("register-username").value,
    );
    const fullName = sanitizeInput(
      document.getElementById("register-fullName").value,
    );
    const email = sanitizeInput(
      document.getElementById("register-email").value,
    );
    const phoneNumber = sanitizeInput(
      document.getElementById("register-phoneNumber")?.value,
    );
    const password = document.getElementById("register-password").value;
    const termsChecked = document.getElementById("terms-conditions")?.checked;
    const language = getCurrentLanguage();

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

    if (phoneNumber && !/^[0-9]{10,11}$/.test(phoneNumber.replace(/\D/g, ""))) {
      errors.push(window.i18next.t("phoneNumberInvalidFormat"));
    }

    if (!termsChecked) {
      errors.push(window.i18next.t("mustAgreeToTerms"));
    }

    if (errors.length > 0) {
      window.showToastr(errors.filter(Boolean).join(", "), "error");
      return;
    }

    try {
      const res = await fetch(`${API_BASE_URL}/Auth/register`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          username,
          fullName: fullName || null,
          email,
          phoneNumber: phoneNumber || null,
          password,
          language,
        }),
      });
      const data = await res.json();

      if (res.ok) {
        if (data.username && data.username !== username) {
          window.showToastr(
            window.i18next
              .t("usernameAutoGenerated")
              .replace("{original}", username)
              .replace("{generated}", data.username),
            "error",
          );
        } else {
          window.showToastr(
            window.i18next.t("registrationSuccessfulCheckEmail"),
            "success",
          );
        }
        setTimeout(() => {
          window.location.href = "/auth/verify-email.html";
        }, 1000);
      } else {
        if (data.errors && Array.isArray(data.errors)) {
          window.showToastr(
            data.errors.map((e) => window.i18next.t(e)).join(", "),
            "error",
          );
        } else {
          const errorMessage = data.message || "registrationFailed";
          if (errorMessage.includes("already exists")) {
            if (errorMessage.includes("Username")) {
              window.showToastr(
                window.i18next
                  .t("usernameAlreadyExists")
                  .replace("{username}", username),
                "error",
              );
            } else {
              window.showToastr(
                window.i18next.t("userAlreadyExists").replace("{email}", email),
                "error",
              );
            }
          } else {
            window.showToastr(window.i18next.t(errorMessage), "error");
          }
        }
      }
    } catch (err) {
      console.error("Register error:", err);
      window.showToastr(
        window.i18next.t("registrationFailedTryAgain"),
        "error",
      );
    }
  });
}

if (document.getElementById("forgot-password-form")) {
  const forgotPasswordForm = document.getElementById("forgot-password-form");
  forgotPasswordForm.addEventListener("submit", async function (e) {
    e.preventDefault();

    const email = sanitizeInput(
      document.getElementById("forgot-password-email").value,
    );
    const language = getCurrentLanguage();

    if (!email) {
      window.showToastr(window.i18next.t("emailRequired"), "error");
      return;
    }

    if (!isValidEmail(email)) {
      window.showToastr(
        window.i18next.t("pleaseEnterValidEmailAddress"),
        "error",
      );
      return;
    }

    try {
      const res = await fetch(`${API_BASE_URL}/Auth/forgot-password`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, language }),
      });
      const data = await res.json();

      if (res.ok) {
        localStorage.setItem("pendingResetEmail", email);
        window.showToastr(
          window.i18next.t("resetEmailSentCheckEmail"),
          "success",
        );
        setTimeout(() => {
          window.location.href = "/auth/login.html";
        }, 1500);
        forgotPasswordForm.reset();
      } else {
        window.showToastr(
          window.i18next.t(data.message || "failedToSendResetEmail"),
          "error",
        );
      }
    } catch (err) {
      console.error("Forgot password error:", err);
      window.showToastr(
        window.i18next.t("failedToSendResetEmailTryAgain"),
        "error",
      );
    }
  });
}

if (document.getElementById("reset-password-form")) {
  const resetPasswordForm = document.getElementById("reset-password-form");
  resetPasswordForm.addEventListener("submit", async function (e) {
    e.preventDefault();

    const password = document.getElementById("reset-password-password").value;
    const confirmPassword = document.getElementById(
      "reset-password-confirm-password",
    ).value;
    const language = getCurrentLanguage();

    if (!password) {
      window.showToastr(window.i18next.t("passwordRequired"), "error");
      return;
    }

    if (!isValidPassword(password)) {
      window.showToastr(window.i18next.t("passwordInvalid"), "error");
      return;
    }

    if (password !== confirmPassword) {
      window.showToastr(window.i18next.t("passwordsDoNotMatch"), "error");
      return;
    }

    const urlParams = new URLSearchParams(window.location.search);
    const token = urlParams.get("token");

    if (!token) {
      window.showToastr(window.i18next.t("invalidResetToken"), "error");
      return;
    }

    try {
      const res = await fetch(`${API_BASE_URL}/Auth/reset-password`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          token: token,
          newPassword: password,
          confirmPassword: confirmPassword,
          language,
        }),
      });
      const data = await res.json();

      if (res.ok) {
        window.showToastr(
          window.i18next.t("passwordResetSuccessRedirectLogin"),
          "success",
        );
        localStorage.removeItem("pendingResetEmail");
        setTimeout(() => {
          window.location.href = "/auth/login.html";
        }, 2000);
      } else {
        window.showToastr(
          window.i18next.t(data.message || "passwordResetFailed"),
          "error",
        );
      }
    } catch (err) {
      window.showToastr(
        window.i18next.t("passwordResetFailedTryAgain"),
        "error",
      );
    }
  });
}

if (document.getElementById("change-password-form")) {
  const changePasswordForm = document.getElementById("change-password-form");
  changePasswordForm.addEventListener("submit", async function (e) {
    e.preventDefault();

    const currentPassword = document.getElementById(
      "change-password-current",
    ).value;
    const newPassword = document.getElementById("change-password-new").value;
    const confirmPassword = document.getElementById(
      "change-password-confirm",
    ).value;
    const language = getCurrentLanguage();

    if (!currentPassword) {
      window.showToastr(window.i18next.t("currentPasswordRequired"), "error");
      return;
    }

    if (!newPassword) {
      window.showToastr(window.i18next.t("newPasswordRequired"), "error");
      return;
    }

    if (!isValidPassword(newPassword)) {
      window.showToastr(window.i18next.t("newPasswordInvalid"), "error");
      return;
    }

    if (newPassword !== confirmPassword) {
      window.showToastr(window.i18next.t("newPasswordsDoNotMatch"), "error");
      return;
    }

    const token = localStorage.getItem("authToken");
    if (!token) {
      window.showToastr(
        window.i18next.t("mustBeLoggedInToChangePassword"),
        "error",
      );
      return;
    }

    try {
      const res = await fetch(`${API_BASE_URL}/Auth/change-password`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          currentPassword,
          newPassword,
          confirmPassword,
          language,
        }),
      });
      const data = await res.json();

      if (res.ok) {
        window.showToastr(
          window.i18next.t("passwordChangedSuccessfully"),
          "success",
        );
        changePasswordForm.reset();
      } else {
        window.showToastr(
          window.i18next.t(data.message || "passwordChangeFailed"),
          "error",
        );
      }
    } catch (err) {
      window.showToastr(
        window.i18next.t("passwordChangeFailedTryAgain"),
        "error",
      );
    }
  });
}

window.addEventListener("DOMContentLoaded", function () {
  if (
    !window.location.pathname.endsWith("verify-email.html") &&
    !window.location.pathname.endsWith("verify-email")
  )
    return;
  const verifySuccessSection = document.getElementById(
    "verify-success-section",
  );
  if (verifySuccessSection) verifySuccessSection.style.display = "block";
});
window.addEventListener("DOMContentLoaded", async function () {
  if (
    !window.location.pathname.endsWith("account-activated.html") &&
    !window.location.pathname.endsWith("account-activated")
  )
    return;
  const urlParams = new URLSearchParams(window.location.search);
  const token = urlParams.get("token");
  const countdownElem = document.getElementById("countdown");
  let countdown = 5;
  function startCountdown() {
    if (countdownElem) countdownElem.textContent = countdown;
    const interval = setInterval(() => {
      countdown--;
      if (countdownElem) countdownElem.textContent = countdown;
      if (countdown <= 0) {
        clearInterval(interval);
        window.location.href = "/auth/login.html";
      }
    }, 1000);
  }
  if (!token) {
    window.showToastr(window.i18next.t("invalidOrExpiredToken"), "error");
    if (countdownElem) countdownElem.textContent = "-";
    return;
  }
  try {
    const res = await fetch(
      `${API_BASE_URL}/Auth/verify-email?token=${encodeURIComponent(token)}`,
    );
    const data = await res.json();
    if (res.ok && data.success) {
      window.showToastr(
        window.i18next.t("emailVerifiedSuccessfully"),
        "success",
      );
      startCountdown();
    } else {
      let msg = data.message || "invalidOrExpiredToken";
      window.showToastr(window.i18next.t(msg), "error");
      if (countdownElem) countdownElem.textContent = "-";
    }
  } catch (err) {
    window.showToastr(window.i18next.t("verifyEmailFailed"), "error");
    if (countdownElem) countdownElem.textContent = "-";
  }
});

function getCurrentLanguage() {
  return window.i18next?.language || localStorage.getItem("i18nextLng") || "en";
}
