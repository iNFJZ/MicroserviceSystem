if (typeof toastr !== "undefined") {
  toastr.options = {
    closeButton: true,
    debug: false,
    newestOnTop: false,
    progressBar: true,
    positionClass: "toast-top-right",
    preventDuplicates: true,
    onclick: null,
    showDuration: 300,
    hideDuration: 1000,
    timeOut: 5000,
    extendedTimeOut: 1000,
    showEasing: "swing",
    hideEasing: "linear",
    showMethod: "fadeIn",
    hideMethod: "fadeOut",
  };
}

window.showToastr = function (message, type = "success", options = {}) {
  if (typeof toastr === "undefined") {
    console.error("Toastr is not loaded!");
    return;
  }
  try {
    // Show toast
    if (options && typeof options === "object") {
      toastr.options = Object.assign({}, toastr.options, options);
    }
    switch (type) {
      case "success":
        toastr.success(message);
        break;
      case "error":
        toastr.error(message);
        break;
      case "info":
        toastr.info(message);
        break;
      case "warning":
        toastr.warning(message);
        break;
      default:
        toastr.success(message);
    }
    // Move toast container to body and force style
    setTimeout(function() {
      var el = document.querySelector('.toast-top-right');
      if (el && el.parentNode !== document.body) {
        document.body.appendChild(el);
      }
      if (el) {
        el.style.zIndex = '99999';
        el.style.width = '350px';
        el.style.maxWidth = '90vw';
        el.style.minWidth = '200px';
        el.style.top = '2.5rem';
        el.style.right = '2.5rem';
        el.style.left = 'auto';
        el.style.marginRight = '0';
        el.style.marginTop = '0';
        el.style.pointerEvents = 'none';
        el.style.boxSizing = 'border-box';
        el.style.position = 'fixed';
      }
    }, 10);
  } catch (e) {
    console.error("showToastr error:", e);
  }
};

document.addEventListener("DOMContentLoaded", function () {
  if (window.i18next && typeof window.i18next.on === "function") {
    window.i18next.on("languageChanged", function () {
      // if (typeof toastr !== "undefined") toastr.clear();
    });
  }
});
