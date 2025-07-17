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
  if (typeof toastr === "undefined") return;
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
};
