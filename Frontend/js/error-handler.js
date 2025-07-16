/**
 * Error Handler for Multi-language Support
 * Handles error codes from backend and displays localized messages
 */

class ErrorHandler {
  constructor() {
    this.currentLanguage =
      window.i18next?.language || localStorage.getItem("i18nextLng") || "en";
    this.langData = null;
    this.loadLanguageData();
  }

  /**
   * Load language data from JSON files
   */
  async loadLanguageData() {
    try {
      const response = await fetch(`/assets/lang/${this.currentLanguage}.json`);
      this.langData = await response.json();
    } catch (error) {
      console.error("Failed to load language data:", error);
      try {
        const response = await fetch("/assets/lang/en.json");
        this.langData = await response.json();
      } catch (fallbackError) {
        console.error("Failed to load fallback language data:", fallbackError);
      }
    }
  }

  /**
   * Get localized error message from error code
   * @param {string} errorCode - The error code from backend
   * @param {Object} params - Optional parameters for message formatting
   * @returns {string} Localized error message
   */
  getErrorMessage(errorCode, params = {}) {
    if (!this.langData || !this.langData.errorCodes) {
      return errorCode || "Unknown error";
    }

    let message = this.langData.errorCodes[errorCode];

    if (!message) {
      // If error code not found, return the error code itself
      return errorCode || "Unknown error";
    }

    // Replace parameters in message
    Object.keys(params).forEach((key) => {
      const placeholder = `{${key}}`;
      message = message.replace(new RegExp(placeholder, "g"), params[key]);
    });

    return message;
  }

  /**
   * Display error message using toastr
   * @param {string} errorCode - The error code from backend
   * @param {Object} params - Optional parameters for message formatting
   * @param {string} type - Error type (error, warning, info, success)
   */
  showError(errorCode, params = {}, type = "error") {
    const message = this.getErrorMessage(errorCode, params);

    if (typeof window.showToastr !== "undefined") {
      window.showToastr(message, type);
    } else {
      alert(message);
    }
  }

  /**
   * Handle API error response
   * @param {Object} response - API response object
   * @param {string} fallbackMessage - Fallback message if no error code
   */
  handleApiError(response, fallbackMessage = "An error occurred") {
    let errorCode = null;
    let params = {};

    // Try to extract error code from different response formats
    if (response.errorCode) {
      errorCode = response.errorCode;
    } else if (response.ErrorCode) {
      errorCode = response.ErrorCode;
    } else if (response.error && response.error.code) {
      errorCode = response.error.code;
    } else if (response.message && response.message.startsWith("ERROR_")) {
      errorCode = response.message;
    } else if (response.errors && response.errors.length > 0) {
      // Handle validation errors
      const firstError = response.errors[0];
      errorCode = firstError.code || "VALIDATION_ERROR";
      params = firstError.params || {};
    }

    if (errorCode) {
      this.showError(errorCode, params);
    } else {
      const message =
        response.message || response.error?.message || fallbackMessage;
      if (typeof window.showToastr !== "undefined") {
        window.showToastr(message, "error");
      } else {
        alert(message);
      }
    }
  }

  /**
   * Handle HTTP status codes
   * @param {number} status - HTTP status code
   * @param {Object} response - Response object
   */
  handleHttpError(status, response) {
    const statusErrorMap = {
      400: "BAD_REQUEST",
      401: "UNAUTHORIZED",
      403: "FORBIDDEN",
      404: "NOT_FOUND",
      409: "CONFLICT",
      422: "VALIDATION_ERROR",
      429: "RATE_LIMIT_EXCEEDED",
      500: "INTERNAL_SERVER_ERROR",
      502: "BAD_GATEWAY",
      503: "SERVICE_UNAVAILABLE",
      504: "GATEWAY_TIMEOUT",
    };

    const errorCode = statusErrorMap[status];
    if (errorCode) {
      this.showError(errorCode);
    } else {
      this.showError("UNKNOWN_ERROR");
    }
  }

  /**
   * Update language and reload language data
   * @param {string} language - Language code (en, vi, ja)
   */
  async updateLanguage(language) {
    this.currentLanguage = language;
    await this.loadLanguageData();
  }

  /**
   * Get success message
   * @param {string} messageKey - Message key from language file
   * @param {Object} params - Optional parameters
   * @returns {string} Localized success message
   */
  getSuccessMessage(messageKey, params = {}) {
    if (!this.langData) {
      return messageKey;
    }

    let message = this.langData[messageKey];
    if (!message) {
      return messageKey;
    }

    // Replace parameters in message
    Object.keys(params).forEach((key) => {
      const placeholder = `{${key}}`;
      message = message.replace(new RegExp(placeholder, "g"), params[key]);
    });

    return message;
  }

  /**
   * Show success message
   * @param {string} messageKey - Message key from language file
   * @param {Object} params - Optional parameters
   */
  showSuccess(messageKey, params = {}) {
    const message = this.getSuccessMessage(messageKey, params);

    if (typeof window.showToastr !== "undefined") {
      window.showToastr(message, "success");
    } else {
      alert(message);
    }
  }

  /**
   * Show warning message
   * @param {string} messageKey - Message key from language file
   * @param {Object} params - Optional parameters
   */
  showWarning(messageKey, params = {}) {
    const message = this.getSuccessMessage(messageKey, params);

    if (typeof window.showToastr !== "undefined") {
      window.showToastr(message, "warning");
    } else {
      alert(message);
    }
  }

  /**
   * Show info message
   * @param {string} messageKey - Message key from language file
   * @param {Object} params - Optional parameters
   */
  showInfo(messageKey, params = {}) {
    const message = this.getSuccessMessage(messageKey, params);

    if (typeof window.showToastr !== "undefined") {
      window.showToastr(message, "info");
    } else {
      alert(message);
    }
  }
}

// Create global instance
window.errorHandler = new ErrorHandler();

// Export for module systems
if (typeof module !== "undefined" && module.exports) {
  module.exports = ErrorHandler;
}
