// Admin Authentication Handler - Common for all admin pages
class AdminAuth {
  constructor() {
    this.isInitialized = false;
    this.init();
  }

  init() {
    if (this.isInitialized) return;
    this.checkAuthentication();
    this.setupLogoutHandler();
    this.setupGlobalErrorHandling();
    this.isInitialized = true;
  }

  checkAuthentication() {
    const token = localStorage.getItem("authToken");
    const currentPath = window.location.pathname;
    const isAdminPage =
      currentPath.includes("/admin/") || currentPath.includes("index.html");
    if (isAdminPage && !token) {
      this.redirectToLogin();
      return false;
    }
    if (isAdminPage && token) {
      this.validateToken(token).catch((error) => {
        console.error("Token validation failed:", error);
      });
    }
    return true;
  }

  async validateToken(token) {
    try {
      const response = await fetch("http://localhost:5050/api/Auth/validate", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ token: token }),
      });
      if (!response.ok) {
        return false;
      }
      const data = await response.json();
      if (!data.isValid) {
        localStorage.removeItem("authToken");
        this.redirectToLogin();
        return false;
      }
      return true;
    } catch (error) {
      return false;
    }
  }

  setupLogoutHandler() {
    $(document).off("click", "#logout-btn");
    $(document).on("click", "#logout-btn", (e) => {
      e.preventDefault();
      e.stopPropagation();
      this.logout();
    });
  }

  async logout() {
    const token = localStorage.getItem("authToken");
    try {
      if (token) {
        await fetch("http://localhost:5050/api/Auth/logout", {
          method: "POST",
          headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "application/json",
          },
        });
      }
    } catch (error) {
      // Only log real errors
      console.error("Logout API error:", error);
    }
    localStorage.removeItem("authToken");
    sessionStorage.clear();
    if (typeof toastr !== "undefined") {
      toastr.success("Logged out successfully!");
    }
    setTimeout(() => {
      this.redirectToLogin();
    }, 500);
  }

  redirectToLogin() {
    window.location.href = "/";
  }

  setupGlobalErrorHandling() {
    $(document).ajaxError((event, xhr, settings) => {
      if (xhr.status === 401) {
        localStorage.removeItem("authToken");
        this.redirectToLogin();
      }
    });
  }

  getAuthToken() {
    return localStorage.getItem("authToken");
  }

  isAuthenticated() {
    return !!localStorage.getItem("authToken");
  }

  getAuthHeaders() {
    const token = this.getAuthToken();
    return token ? { Authorization: `Bearer ${token}` } : {};
  }

  getCurrentUserInfo() {
    const token = this.getAuthToken();
    if (!token) return null;

    try {
      const payload = JSON.parse(
        atob(token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")),
      );
      return {
        id: payload.nameid || payload.sub,
        email: payload.email,
        username: payload.username,
        fullName: payload.fullName || payload.name,
        role: payload.role || "Admin",
      };
    } catch (e) {
      console.error("Failed to parse JWT token:", e);
      return null;
    }
  }

  async updateUserProfileDisplay() {
    const userInfo = this.getCurrentUserInfo();
    if (!userInfo) {
      $(".user-name").text("Guest");
      $(".user-role").text("User");
      $(".user-avatar")
        .attr(
          "src",
          generateLetterAvatarFromUser({ email: "guest@system.com" }),
        )
        .show();
      return;
    }

    $(".user-name").text(userInfo.fullName || userInfo.username || "Admin");
    $(".user-role").text(userInfo.role || "Admin");
    $(".user-avatar")
      .attr("src", generateLetterAvatarFromUser(userInfo))
      .show();

    try {
      const response = await fetch(
        `http://localhost:5050/api/User/${userInfo.id}`,
        {
          headers: this.getAuthHeaders(),
        },
      );
      if (response.ok) {
        const data = await response.json();
        if (data.success && data.data) {
          const user = data.data;
          if (user.profilePicture && user.profilePicture.trim() !== "") {
            $(".user-avatar").attr("src", user.profilePicture).show();
          } else {
            $(".user-avatar")
              .attr("src", generateLetterAvatarFromUser(user))
              .show();
          }
          if (user.fullName && user.fullName.trim() !== "") {
            $(".user-name").text(user.fullName);
          }
        }
      }
    } catch (error) {
      console.error("Failed to fetch user details:", error);
    }
  }
}

$(document).ready(async () => {
  window.adminAuth = new AdminAuth();
  if (window.adminAuth) {
    await window.adminAuth.updateUserProfileDisplay();
  }
});

if (typeof module !== "undefined" && module.exports) {
  module.exports = AdminAuth;
}

function loadActiveUsersTable() {
  const token = localStorage.getItem("authToken");
  if (!token) {
    window.location.href = "/";
    return;
  }
  const dt_user_table = $(".datatables-users");
  if (dt_user_table.length) {
    dt_user_table.DataTable().destroy();
    dt_user_table.DataTable({
      ajax: {
        url: "http://localhost:5050/api/User",
        dataSrc: function (json) {
          if (!json || !Array.isArray(json.data)) return [];
          const users = json.data;
          const activeUsers = users.filter(
            (u) => u.status === 1 || u.status === "Active",
          );
          let total = activeUsers.length,
            active = total,
            inactive = 0,
            suspended = 0,
            banned = 0;
          $("#total-users").text(total);
          $("#active-users").text(active);
          $("#inactive-users").text(inactive);
          $("#banned-users").text(banned);
          return activeUsers;
        },
        beforeSend: function (xhr) {
          if (token) xhr.setRequestHeader("Authorization", "Bearer " + token);
        },
      },
      columns: [
        { data: null },
        { data: "username" },
        { data: "fullName" },
        { data: "email" },
        { data: "phoneNumber" },
        { data: "status" },
        { data: "lastLoginAt" },
        { data: null },
      ],
      columnDefs: getUserTableColumnDefs(),
      order: [[1, "asc"]],
      dom: getUserTableDom(),
      language: getUserTableLanguage(),
      buttons: getUserTableButtons(),
      rowCallback: function (row, data) {
        $(row).attr("data-userid", data.id);
      },
    });
  }
}

function loadAllUsersTable() {
  const token = localStorage.getItem("authToken");
  if (!token) {
    window.location.href = "/";
    return;
  }
  const dt_user_table = $(".datatables-users");
  if (dt_user_table.length) {
    dt_user_table.DataTable().destroy();
    dt_user_table.DataTable({
      ajax: {
        url: "http://localhost:5050/api/User?includeDeleted=true",
        dataSrc: function (json) {
          if (!json || !Array.isArray(json.data)) {
            return [];
          }
          const users = json.data;
          let total = users.length,
            active = 0,
            inactive = 0,
            suspended = 0,
            banned = 0;
          users.forEach((u) => {
            if (u.status === 1 || u.status === "Active") active++;
            else if (u.status === 2 || u.status === "Inactive") inactive++;
            else if (u.status === 3 || u.status === "Suspended") suspended++;
            else if (u.status === 4 || u.status === "Banned") banned++;
          });
          $("#total-users").text(total);
          $("#active-users").text(active);
          $("#inactive-users").text(inactive);
          $("#banned-users").text(banned);
          return users;
        },
        beforeSend: function (xhr) {
          const token = localStorage.getItem("authToken");
          if (token) xhr.setRequestHeader("Authorization", "Bearer " + token);
        },
      },
      columns: [
        { data: null },
        { data: "username" },
        { data: "fullName" },
        { data: "email" },
        { data: "phoneNumber" },
        { data: "status" },
        { data: null },
        { data: null },
        { data: null },
      ],
      columnDefs: getUserTableColumnDefs(),
      createdRow: function (row, data) {
        $(row).attr("data-userid", data.id);
      },
      order: [[1, "asc"]],
      dom: getUserTableDom(),
      language: getUserTableLanguage(),
      buttons: getUserTableButtons(),
    });
  }
}

function loadDeactiveUsersTable() {
  const token = localStorage.getItem("authToken");
  if (!token) {
    window.location.href = "/";
    return;
  }
  const dt_user_table = $(".datatables-users");
  if (dt_user_table.length) {
    dt_user_table.DataTable().destroy();
    dt_user_table.DataTable({
      ajax: {
        url: "http://localhost:5050/api/User?includeDeleted=true",
        dataSrc: function (json) {
          if (!json || !Array.isArray(json.data)) return [];
          const users = json.data;
          const deactiveUsers = users.filter(
            (u) => u.status === 4 || u.status === "Banned" || u.deletedAt,
          );
          return deactiveUsers;
        },
        beforeSend: function (xhr) {
          if (token) xhr.setRequestHeader("Authorization", "Bearer " + token);
        },
      },
      columns: [
        { data: null },
        { data: "username" },
        { data: "fullName" },
        { data: "email" },
        { data: "phoneNumber" },
        { data: "status" },
        { data: "deletedAt" },
        { data: null },
      ],
      columnDefs: getUserTableColumnDefs(true),
      createdRow: function (row, data) {
        $(row).attr("data-userid", data.id);
      },
      order: [[1, "asc"]],
      dom: getUserTableDom(),
      language: getUserTableLanguage(),
      buttons: getUserTableButtons(),
    });
  }
}

function getUserTableColumnDefs(isDeactive) {
  return [
    {
      targets: 0, // Avatar
      render: function (data, type, full) {
        if (full.profilePicture && full.profilePicture.trim() !== "") {
          return `<img src="${full.profilePicture}" alt="avatar" class="rounded-circle" style="width:36px;height:36px;object-fit:cover;">`;
        } else {
          const letter = (full.username || "").charAt(0).toUpperCase();
          const color = "#" + (((1 << 24) * Math.random()) | 0).toString(16);
          return `<div class="avatar-initial rounded-circle" style="width:36px;height:36px;background:${color};color:#fff;display:flex;align-items:center;justify-content:center;font-weight:bold;font-size:18px;">${letter}</div>`;
        }
      },
    },
    {
      targets: 1, // Username
      render: function (data, type, full) {
        return full.username || "";
      },
    },
    {
      targets: 2, // Full Name
      render: function (data, type, full) {
        return full.fullName || "";
      },
    },
    {
      targets: 3, // Email
      render: function (data, type, full) {
        return full.email || "";
      },
    },
    {
      targets: 4, // Phone
      render: function (data, type, full) {
        return full.phoneNumber || "";
      },
    },
    {
      targets: 5, // Status
      render: function (data, type, full) {
        var $status = full.status;
        var statusObj = {
          1: { title: "Active", class: "bg-label-success" },
          2: { title: "Inactive", class: "bg-label-secondary" },
          3: { title: "Suspended", class: "bg-label-warning" },
          4: { title: "Banned", class: "bg-label-danger" },
        };
        var obj = statusObj[$status] || {
          title: "Unknown",
          class: "bg-label-secondary",
        };
        return `<span class="badge ${obj.class}" text-capitalized>${obj.title}</span>`;
      },
    },
    {
      targets: 6,
      render: function (data, type, full) {
        if (isDeactive) {
          if (full.deletedAt || full.DeletedAt) {
            return new Date(full.deletedAt || full.DeletedAt).toLocaleString(
              "en-GB",
              {
                year: "numeric",
                month: "2-digit",
                day: "2-digit",
                hour: "2-digit",
                minute: "2-digit",
                second: "2-digit",
              },
            );
          } else {
            return '<span class="text-muted">N/A</span>';
          }
        } else {
          if (full.lastLoginAt) {
            return new Date(full.lastLoginAt).toLocaleString("en-GB", {
              year: "numeric",
              month: "2-digit",
              day: "2-digit",
              hour: "2-digit",
              minute: "2-digit",
              second: "2-digit",
            });
          } else {
            return '<span class="text-muted">N/A</span>';
          }
        }
      },
    },
    {
      targets: 7, // Actions
      title: "Actions",
      searchable: false,
      orderable: false,
      render: function (data, type, row, meta) {
        let html = "";
        html += `<a href="javascript:;" class="text-body view-user" title="View User"><i class="ti ti-eye text-primary me-1"></i></a>`;
        if (isDeactive) {
          html += `<a href="javascript:;" class="text-body restore-user" title="Restore User"><i class="ti ti-refresh text-success me-1"></i></a>`;
        } else {
          html += `<a href="javascript:;" class="text-body edit-user" title="Edit User"><i class="ti ti-edit text-primary me-1"></i></a>`;
          html += `<a href="javascript:;" class="text-body delete-user" title="Delete User"><i class="ti ti-trash text-danger me-1"></i></a>`;
        }
        return html;
      },
    },
  ];
}

function getUserTableDom() {
  return '<"row me-2"<"col-md-2"<"me-3"l>><"col-md-10"<"dt-action-buttons text-xl-end text-lg-start text-md-end text-start d-flex align-items-center justify-content-end flex-md-row flex-column mb-3 mb-md-0"fB>>>t<"row mx-2"<"col-sm-12 col-md-6"i><"col-sm-12 col-md-6"p>>';
}

function getUserTableLanguage() {
  return {
    sLengthMenu: "_MENU_",
    search: "",
    searchPlaceholder: "Search..",
  };
}

function getUserTableButtons() {
  return [
    {
      extend: "collection",
      className: "btn btn-label-secondary dropdown-toggle mx-3",
      text: '<i class="ti ti-screen-share me-1 ti-xs"></i>Export',
      buttons: [
        {
          extend: "print",
          text: '<i class="ti ti-printer me-2" ></i>Print',
          className: "dropdown-item",
          exportOptions: {
            columns: [1, 2, 3, 4, 5],
            format: {
              body: function (inner, coldex, rowdex) {
                return $(inner).text();
              },
            },
          },
          customize: function (win) {
            $(win.document.body).css("color", "#697a8d");
          },
        },
        {
          extend: "csv",
          text: '<i class="ti ti-file-text me-2" ></i>Csv',
          className: "dropdown-item",
          exportOptions: {
            columns: [1, 2, 3, 4, 5],
            format: {
              body: function (inner, coldex, rowdex) {
                return $(inner).text();
              },
            },
          },
        },
        {
          extend: "excel",
          text: '<i class="ti ti-file-spreadsheet me-2"></i>Excel',
          className: "dropdown-item",
          exportOptions: {
            columns: [1, 2, 3, 4, 5],
            format: {
              body: function (inner, coldex, rowdex) {
                return $(inner).text();
              },
            },
          },
        },
        {
          extend: "pdf",
          text: '<i class="ti ti-file-code-2 me-2"></i>Pdf',
          className: "dropdown-item",
          exportOptions: {
            columns: [1, 2, 3, 4, 5],
            format: {
              body: function (inner, coldex, rowdex) {
                return $(inner).text();
              },
            },
          },
        },
        {
          extend: "copy",
          text: '<i class="ti ti-copy me-2" ></i>Copy',
          className: "dropdown-item",
          exportOptions: {
            columns: [1, 2, 3, 4, 5],
            format: {
              body: function (inner, coldex, rowdex) {
                return $(inner).text();
              },
            },
          },
        },
      ],
    },
  ];
}

function isValidEmail(email) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

function handleAddUser() {
  const form = document.getElementById("addNewUserForm");
  if (!form) return;
  const formData = new FormData(form);
  const fullName = formData.get("fullName")?.trim();
  const email = formData.get("email")?.trim();
  const phoneNumber = formData.get("phoneNumber")?.trim();
  if (!fullName) {
    toastr.error("Full name is required!");
    return;
  }
  if (!email || !isValidEmail(email)) {
    toastr.error("Valid email is required!");
    return;
  }
  const data = { fullName, email };
  if (phoneNumber) data.phoneNumber = phoneNumber;
  const token = localStorage.getItem("authToken");
  fetch("http://localhost:5050/api/User", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify(data),
  })
    .then((res) => res.json())
    .then((res) => {
      if (res.success || res.id) {
        toastr.success("User added successfully!");
        form.reset();
        $("#offcanvasAddUser").offcanvas("hide");
        loadAllUsersTable && loadAllUsersTable();
        loadActiveUsersTable && loadActiveUsersTable();
        loadDeactiveUsersTable && loadDeactiveUsersTable();
      } else {
        toastr.error(res.message || "Add user failed!");
      }
    })
    .catch(() => toastr.error("Add user failed!"));
}

// --- CropperJS integration ---
let cropper = null;
let selectedImageFile = null;
let windowCropper = null;
let currentZoom = 1;
let cropperReady = false;
let initialCropBoxWidth = null;

$(document).on(
  "change",
  "#edit-profilePicture, #profile-picture-input",
  function (e) {
    const file = this.files && this.files[0];
    if (file) {
      if (!file.type.startsWith("image/")) {
        toastr.error("Please select a valid image file!");
        this.value = "";
        return;
      }
      if (file.size > 5 * 1024 * 1024) {
        toastr.error("Image size must be less than 5MB!");
        this.value = "";
        return;
      }
      if (file.size < 10 * 1024) {
        toastr.warning(
          "Image size is very small. For better quality, use an image larger than 10KB.",
        );
      }
      selectedImageFile = file;
      const reader = new FileReader();
      reader.onload = function (ev) {
        const $img = $("#cropper-image");
        $img.attr("src", ev.target.result);
        $img.off("load").on("load", function () {
          if (cropper) {
            cropper.destroy();
            cropper = null;
          }
          $("#cropImageModal").modal("show");
          $("#cropImageModal").one("shown.bs.modal", function () {
            setTimeout(() => {
              initializeCropper($img[0], ev.target.result);
            }, 100);
          });
        });
        if ($img[0].complete) {
          $img.trigger("load");
        }
      };
      reader.readAsDataURL(file);
    } else {
      $("#edit-profilePicture-container, #profile-picture-preview").hide();
      window._editProfilePictureBase64 = null;
    }
  },
);

function initializeCropper(imageElement, imageUrl) {
  $(".drag-drop-zone").addClass("hidden");
  currentZoom = 1;
  cropperReady = false;
  initialCropBoxWidth = null;
  cropper = new Cropper(imageElement, {
    aspectRatio: 1,
    viewMode: 1,
    dragMode: "crop",
    autoCropArea: 0.8,
    background: false,
    responsive: true,
    movable: false,
    rotatable: true,
    scalable: false,
    zoomable: true,
    zoomOnWheel: true,
    wheelZoomRatio: 0.1,
    cropBoxMovable: true,
    cropBoxResizable: true,
    toggleDragModeOnDblclick: false,
    ready: function () {
      let triedResize = false;
      let tryCount = 0;
      function waitForCropBox() {
        tryCount++;
        const cropBox = cropper.getCropBoxData();
        const imageData = cropper.getImageData();
        if (!imageData || !cropBox) return setTimeout(waitForCropBox, 30);
        if (tryCount === 1) {
          const boxSize = Math.floor(Math.min(imageData.naturalWidth, imageData.naturalHeight) * 0.8);
          cropper.setCropBoxData({
            width: boxSize,
            height: boxSize,
            left: imageData.left + (imageData.naturalWidth - boxSize) / 2,
            top: imageData.top + (imageData.naturalHeight - boxSize) / 2
          });
          setTimeout(waitForCropBox, 30);
          return;
        }
        let adjusted = false;
        let newLeft = cropBox.left;
        let newTop = cropBox.top;
        if (cropBox.left < imageData.left) {
          newLeft = imageData.left;
          adjusted = true;
        }
        if (cropBox.top < imageData.top) {
          newTop = imageData.top;
          adjusted = true;
        }
        if (cropBox.left + cropBox.width > imageData.left + imageData.naturalWidth) {
          newLeft = imageData.left + imageData.naturalWidth - cropBox.width;
          adjusted = true;
        }
        if (cropBox.top + cropBox.height > imageData.top + imageData.naturalHeight) {
          newTop = imageData.top + imageData.naturalHeight - cropBox.height;
          adjusted = true;
        }
        if (adjusted) {
          cropper.setCropBoxData({
            width: cropBox.width,
            height: cropBox.height,
            left: newLeft,
            top: newTop
          });
          setTimeout(waitForCropBox, 30);
          return;
        }
        if (imageData.naturalWidth < 100 || imageData.naturalHeight < 100) {
          toastr.error("Image is too small. Please use an image at least 100x100px.");
          $("#cropImageModal").modal("hide");
          return;
        }
        if (tryCount > 10 && (!cropBox || cropBox.width <= 0 || cropBox.height <= 0)) {
          toastr.error("Failed to initialize cropper. Please try another image.");
          $("#cropImageModal").modal("hide");
          return;
        }
        if (!initialCropBoxWidth) initialCropBoxWidth = cropBox.width;
        let maxZoom = Math.min(
          Math.floor((initialCropBoxWidth / 10) * 100) / 100,
          imageData.naturalWidth / initialCropBoxWidth,
          3
        );
        maxZoom = Math.max(1, maxZoom);
        cropperReady = true;
        updateZoomSlider(maxZoom);
        updateCircleOverlay();
        updateAvatarPreview();
        const zoomSlider = document.getElementById("zoom-slider");
        if (zoomSlider) {
          zoomSlider.disabled = maxZoom === 1;
          zoomSlider.max = maxZoom.toFixed(2);
        }
        $("#zoom-in-btn, #zoom-out-btn").prop("disabled", maxZoom === 1);
      }
      waitForCropBox();
      const zoomSlider = document.getElementById("zoom-slider");
      if (zoomSlider) {
        zoomSlider.min = 1;
        zoomSlider.max = 3;
        zoomSlider.step = 0.01;
        zoomSlider.value = 1;
        zoomSlider.oninput = function () {
          let val = parseFloat(this.value);
          if (val < 1) val = 1;
          if (val > parseFloat(this.max)) val = parseFloat(this.max);
          if (val === currentZoom) return;
          this.value = val;
          currentZoom = val;
          if (initialCropBoxWidth) {
            const newWidth = initialCropBoxWidth / currentZoom;
            const cropBox = cropper.getCropBoxData();
            cropper.setCropBoxData({
              width: newWidth,
              height: newWidth,
              left: cropBox.left,
              top: cropBox.top
            });
          } else {
            cropper.zoomTo(currentZoom);
          }
          updateZoomDisplay(currentZoom);
        };
      }
      updateZoomSlider();
      updateCircleOverlay();
      updateAvatarPreview();
      $("#zoom-in-btn")
        .off("click")
        .on("click", function () {
          const zoomSlider = document.getElementById("zoom-slider");
          let maxZoom = zoomSlider ? parseFloat(zoomSlider.max) : 3;
          if (currentZoom >= maxZoom) return;
          let newZoom = Math.min(currentZoom + 0.1, maxZoom);
          if (newZoom === currentZoom) return;
          currentZoom = newZoom;
          if (initialCropBoxWidth) {
            const newWidth = initialCropBoxWidth / currentZoom;
            const cropBox = cropper.getCropBoxData();
            cropper.setCropBoxData({
              width: newWidth,
              height: newWidth,
              left: cropBox.left,
              top: cropBox.top
            });
          } else {
            cropper.zoomTo(currentZoom);
          }
          updateZoomSlider(maxZoom);
          updateZoomDisplay(currentZoom);
        });
      $("#zoom-out-btn")
        .off("click")
        .on("click", function () {
          if (currentZoom <= 1) return;
          let newZoom = Math.max(currentZoom - 0.1, 1);
          if (newZoom === currentZoom) return;
          currentZoom = newZoom;
          if (initialCropBoxWidth) {
            const newWidth = initialCropBoxWidth / currentZoom;
            const cropBox = cropper.getCropBoxData();
            cropper.setCropBoxData({
              width: newWidth,
              height: newWidth,
              left: cropBox.left,
              top: cropBox.top
            });
          } else {
            cropper.zoomTo(currentZoom);
          }
          updateZoomSlider();
          updateZoomDisplay(currentZoom);
        });
      $("#rotate-left-btn")
        .off("click")
        .on("click", function () {
          cropper.rotate(-90);
        });
      $("#rotate-right-btn")
        .off("click")
        .on("click", function () {
          cropper.rotate(90);
        });
      $("#reset-btn")
        .off("click")
        .on("click", function () {
          cropper.reset();
          currentZoom = 1;
          updateZoomSlider();
          updateZoomDisplay(currentZoom);
        });
    },
    zoom: function (event) {
      let ratio = event.detail.ratio;
      const zoomSlider = document.getElementById("zoom-slider");
      let maxZoom = zoomSlider ? parseFloat(zoomSlider.max) : 3;
      if (ratio < 1) {
        event.preventDefault();
        currentZoom = 1;
      } else if (ratio > maxZoom) {
        event.preventDefault();
        currentZoom = maxZoom;
      } else {
        currentZoom = ratio;
      }
      if (zoomSlider) {
        zoomSlider.value = currentZoom;
        updateZoomDisplay(currentZoom);
      }
      updateCircleOverlay();
    },
    crop: function (event) {
      updateCircleOverlay();
      if (cropperReady) updateAvatarPreview();
      updateZoomSlider();
    },
    cropmove: function () {
      updateCircleOverlay();
      if (cropperReady) updateAvatarPreview();
    },
    error: function () {
      toastr.error("Failed to load image. Please try another image.");
      $("#cropImageModal").modal("hide");
    }
  });
  window._cropper = cropper;
}
function updateZoomSlider(maxZoom) {
  const zoomSlider = document.getElementById("zoom-slider");
  if (zoomSlider) {
    if (maxZoom) zoomSlider.max = maxZoom;
    zoomSlider.value = currentZoom;
    updateZoomDisplay(currentZoom);
    const minLabel = zoomSlider.parentElement?.previousElementSibling;
    const maxLabel = document.getElementById("zoom-max-label");
    if (minLabel) minLabel.textContent = "100%";
    if (maxLabel && maxZoom) maxLabel.textContent = `${Math.round(maxZoom * 100)}%`;
  }
}

function updateZoomDisplay(zoom) {
  const percentage = Math.round((zoom || 1) * 100);
  const zoomDisplay = document.querySelector(".zoom-percentage");
  if (zoomDisplay) {
    zoomDisplay.textContent = `${percentage}%`;
  }
}

// Drag & Drop functionality
$(document).on("dragover", ".drag-drop-zone", function (e) {
  e.preventDefault();
  $(this).addClass("dragover");
});

$(document).on("dragleave", ".drag-drop-zone", function (e) {
  e.preventDefault();
  $(this).removeClass("dragover");
});

$(document).on("drop", ".drag-drop-zone", function (e) {
  e.preventDefault();
  $(this).removeClass("dragover");

  const files = e.originalEvent.dataTransfer.files;
  if (files.length > 0) {
    const file = files[0];
    if (file.type.startsWith("image/")) {
      handleImageFile(file);
    } else {
      toastr.error("Please select a valid image file!");
    }
  }
});

$(document).on("click", ".drag-drop-zone", function () {
  $("#edit-profilePicture, #profile-picture-input").click();
});

// Handle image file
function handleImageFile(file) {
  if (file.size > 5 * 1024 * 1024) {
    toastr.error("Image size must be less than 5MB!");
    return;
  }
  if (file.size < 10 * 1024) {
    toastr.warning(
      "Image size is very small. For better quality, use an image larger than 10KB.",
    );
  }

  selectedImageFile = file;
  const reader = new FileReader();
  reader.onload = function (ev) {
    const $img = $("#cropper-image");
    if (cropper) {
      cropper.destroy();
      cropper = null;
    }
    $img.attr("src", ev.target.result);
    $img.off("load").on("load", function () {
      initializeCropper($img[0], ev.target.result);
    });
    if ($img[0].complete) {
      $img.trigger("load");
    }
  };
  reader.readAsDataURL(file);
}

function openCropperModal(imageUrl) {
  const $modal = $("#cropImageModal");
  const $img = $("#cropper-image");
  if (cropper) {
    cropper.destroy();
    cropper = null;
  }
  $img.hide();
  $img.attr("src", imageUrl);
  $modal.modal("show");
  $img.off("load").on("load", function () {
    $img.show();
    initializeCropper($img[0], imageUrl);
  });
}

function updateCircleOverlay() {
  if (!cropper) return;
  const overlay = document.querySelector(".crop-circle-overlay");
  if (!overlay) return;
  const cropBox = cropper.getCropBoxData();
  overlay.style.width = `${cropBox.width}px`;
  overlay.style.height = `${cropBox.height}px`;
  overlay.style.left = `${cropBox.left}px`;
  overlay.style.top = `${cropBox.top}px`;
  overlay.style.display = "block";
}

function updateAvatarPreview() {
  if (!cropper || !cropperReady) return;
  let cropBox;
  try {
    cropBox = cropper.getCropBoxData();
    const imageData = cropper.getImageData();
    if (
      cropBox.left < imageData.left ||
      cropBox.top < imageData.top ||
      cropBox.left + cropBox.width > imageData.left + imageData.naturalWidth ||
      cropBox.top + cropBox.height > imageData.top + imageData.naturalHeight
    ) {
      ensureCropBoxInBounds();
      cropBox = cropper.getCropBoxData();
    }
    if (!cropBox || cropBox.width <= 0 || cropBox.height <= 0) return;
    if (imageData && (cropBox.width > imageData.naturalWidth || cropBox.height > imageData.naturalHeight)) {
      const preview = document.getElementById("crop-avatar-preview");
      if (preview) {
        preview.src = "";
        preview.style.display = "none";
      }
      return;
    }
    const canvas = cropper.getCroppedCanvas({
      width: 200,
      height: 200,
      imageSmoothingQuality: "high",
      fillColor: "#fff",
    });
    if (!canvas) return;
    const preview = document.getElementById("crop-avatar-preview");
    if (canvas && preview) {
      const circleCanvas = document.createElement("canvas");
      circleCanvas.width = 200;
      circleCanvas.height = 200;
      const ctx = circleCanvas.getContext("2d");
      ctx.save();
      ctx.beginPath();
      ctx.arc(100, 100, 100, 0, 2 * Math.PI);
      ctx.closePath();
      ctx.clip();
      ctx.drawImage(canvas, 0, 0, 200, 200);
      ctx.restore();
      preview.src = circleCanvas.toDataURL("image/png");
      preview.style.display = "block";
    } else if (preview) {
      preview.src = "";
      preview.style.display = "none";
    }
  } catch (error) {
    const preview = document.getElementById("crop-avatar-preview");
    if (preview) {
      preview.src = "";
      preview.style.display = "none";
    }
  }
}

$("#cropImageModal").on("hidden.bs.modal", function () {
  if (cropper) {
    cropper.destroy();
    cropper = null;
  }
  if (!window._editProfilePictureBase64) {
    $("#edit-profilePicture, #profile-picture-input").val("");
    selectedImageFile = null;
  }
  // Show drag drop zone again
  $(".drag-drop-zone").removeClass("hidden");
});

$(document).on("click", "#cropImageBtn", function () {
  if (!cropper || !cropperReady) {
    toastr.error("Cropper is not ready. Please wait for the image to load.");
    return;
  }
  let cropBox = cropper.getCropBoxData();
  const imageData = cropper.getImageData();
  if (!cropBox || cropBox.width <= 0 || cropBox.height <= 0) {
    toastr.error("Crop area is invalid. Please adjust the crop box.");
    return;
  }
  if (imageData && (cropBox.width > imageData.naturalWidth || cropBox.height > imageData.naturalHeight)) {
    toastr.error("Crop area is larger than the image. Please zoom in or use a larger image.");
    return;
  }
  try {
    const canvas = cropper.getCroppedCanvas({
      width: 200,
      height: 200,
      imageSmoothingEnabled: true,
      imageSmoothingQuality: "high",
      fillColor: "#fff",
    });
    if (!canvas) throw new Error("Canvas is null. Image may be too small or crop area invalid.");
    const size = 200;
    const circleCanvas = document.createElement("canvas");
    circleCanvas.width = size;
    circleCanvas.height = size;
    const ctx = circleCanvas.getContext("2d");
    ctx.save();
    ctx.beginPath();
    ctx.arc(size / 2, size / 2, size / 2, 0, 2 * Math.PI);
    ctx.closePath();
    ctx.clip();
    ctx.drawImage(canvas, 0, 0, size, size);
    ctx.restore();
    window._editProfilePictureBase64 = circleCanvas.toDataURL("image/png");
    $("#crop-avatar-preview, #edit-profilePicture-preview").attr(
      "src",
      window._editProfilePictureBase64,
    );
    $("#edit-profilePicture-container, #profile-picture-preview").show();
    setTimeout(() => {
      $("#cropImageModal").modal("hide");
      toastr.success(
        "Profile picture cropped successfully! The image will be displayed as a circular avatar.",
      );
    }, 200);
  } catch (error) {
    window._editProfilePictureBase64 = null;
    $("#crop-avatar-preview, #edit-profilePicture-preview").attr("src", "");
    $("#edit-profilePicture-container, #profile-picture-preview").hide();
    toastr.error(error.message || "Failed to crop image. Please try again!");
  }
});

$(document).on("click", "#remove-profile-picture", function () {
  $("#crop-avatar-preview, #edit-profilePicture-preview").attr("src", "");
  $("#edit-profilePicture-container, #profile-picture-preview").hide();
  $("#edit-profilePicture, #profile-picture-input").val("");
  window._editProfilePictureBase64 = null;
  selectedImageFile = null;
  window._profilePictureRemoved = true;
  if (cropper) {
    cropper.destroy();
    cropper = null;
  }
  toastr.info("Profile picture removed!");
});

function handleUpdateUser(userId) {
  const form = document.getElementById("editUserForm");
  if (!form) return;
  const formData = new FormData(form);

  let dateOfBirth = formData.get("dateOfBirth");
  const data = {
    fullName: formData.get("fullName")?.trim(),
    phoneNumber: formData.get("phoneNumber")?.trim(),
    address: formData.get("address")?.trim(),
    bio: formData.get("bio")?.trim(),
    status: parseInt(formData.get("status")) || 1,
    isVerified: formData.get("isVerified") === "on",
  };

  if (dateOfBirth && dateOfBirth.trim() !== "") {
    const today = new Date();
    if (dateOfBirth > today.toISOString().split("T")[0]) {
      toastr.error("Date of birth cannot be in the future!");
      return;
    }
    data.dateOfBirth = new Date(dateOfBirth).toISOString();
  }
  if (window._editProfilePictureBase64) {
    data.profilePicture = window._editProfilePictureBase64;
  } else if (selectedImageFile) {
    toastr.warning("Please crop your profile picture before saving!");
    return;
  } else if (window._profilePictureRemoved) {
    data.profilePicture = null;
  }

  Object.keys(data).forEach((key) => {
    if (data[key] === null || data[key] === undefined || data[key] === "") {
      delete data[key];
    }
  });

  if (data.isVerified !== undefined) {
    data.isVerified = Boolean(data.isVerified);
  }

  const original = $("#editUserForm").data("original") || {};
  let changed = false;

  if (
    window._editProfilePictureBase64 ||
    selectedImageFile ||
    window._profilePictureRemoved
  ) {
    changed = true;
  }

  for (const key of Object.keys(data)) {
    if (key === "profilePicture") continue;

    let oldVal = original[key] || "";
    let newVal = data[key] || "";

    if (key === "dateOfBirth") {
      if (oldVal) {
        oldVal = oldVal.split("T")[0];
      }
      if (newVal) {
        newVal = newVal.split("T")[0];
      }
    }

    if (String(oldVal) !== String(newVal)) {
      changed = true;
      break;
    }
  }
  if (!changed) {
    toastr.warning("You have not changed any information!");
    $("#editUserModal").modal("hide");
    return;
  }
  if (data.phoneNumber !== original.phoneNumber) {
    if (data.phoneNumber && !/^[0-9]{10,11}$/.test(data.phoneNumber)) {
      toastr.error("Phone number must be 10-11 digits and only numbers!");
      return;
    }
  }
  if (!data.fullName) {
    toastr.error("Full name is required!");
    return;
  }
  const token = localStorage.getItem("authToken");
  if (!token) {
    toastr.error("Authentication required!");
    return;
  }
  fetch(`http://localhost:5050/api/User/${userId}`, {
    method: "PUT",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify(data),
  })
    .then(async (res) => {
      let responseData;
      try {
        responseData = await res.json();
      } catch (e) {
        responseData = {};
      }
      if (!res.ok) {
        if (responseData.errors) {
          Object.keys(responseData.errors).forEach((field) => {
            const messages = responseData.errors[field];
            if (Array.isArray(messages)) {
              messages.forEach((msg) => toastr.error(`${field}: ${msg}`));
            } else {
              toastr.error(`${field}: ${messages}`);
            }
          });
        } else if (responseData.message) {
          toastr.error(responseData.message);
        } else {
          toastr.error("Failed to update user!");
        }
        throw new Error(
          responseData.message || responseData.error || `HTTP ${res.status}`,
        );
      }
      return responseData;
    })
    .then((res) => {
      toastr.success("User updated successfully!");
      $("#editUserModal").modal("hide");

      reloadCurrentPageData();

      setTimeout(() => {
        const dt_user_table = $(".datatables-users");
        if (dt_user_table.length && $.fn.DataTable.isDataTable(dt_user_table)) {
          dt_user_table.DataTable().ajax.reload(null, false);
        }
      }, 500);

      window._editProfilePictureBase64 = null;
      selectedImageFile = null;
      window._profilePictureRemoved = false;
      if (cropper) {
        cropper.destroy();
        cropper = null;
      }
    })
    .catch(() => toastr.error("Failed to update user!"));
}

function deleteUser(userId) {
  const token = localStorage.getItem("authToken");
  if (!userId || !token) {
    toastr.error("Invalid request!");
    return;
  }

  let currentUserInfo = null;
  try {
    const payload = JSON.parse(
      atob(token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")),
    );
    currentUserInfo = {
      id: payload.nameid || payload.sub,
      email: payload.email,
      username: payload.username,
    };
  } catch (e) {
    console.error("Failed to parse JWT token:", e);
  }

  fetch(`http://localhost:5050/api/User/${userId}`, {
    method: "DELETE",
    headers: { Authorization: `Bearer ${token}` },
  })
    .then(async (res) => {
      let responseData = {};
      try {
        responseData = await res.json();
      } catch {}
      if (!res.ok) {
        throw new Error(responseData.message || `HTTP ${res.status}`);
      }
      return responseData;
    })
    .then((res) => {
      toastr.success("User deleted successfully!");
      $("#deleteUserModal").modal("hide");
      const userInfo = res.data || res;

      if (
        userInfo &&
        currentUserInfo &&
        (userInfo.id == currentUserInfo.id ||
          userInfo.email === currentUserInfo.email ||
          userInfo.username === currentUserInfo.username)
      ) {
        localStorage.removeItem("authToken");
        sessionStorage.clear();
        toastr.info("Your account has been deleted. Redirecting to login...");
        setTimeout(() => {
          window.location.href = "/auth/login.html";
        }, 1000);
        return;
      }
      reloadCurrentPageData();
    })
    .catch((error) => {
      console.error("Delete user error:", error);
      toastr.error(error.message || "Failed to delete user!");
    });
}

function restoreUser(userId) {
  const token = localStorage.getItem("authToken");
  if (!userId || !token) {
    toastr.error("Invalid request!");
    return;
  }

  fetch(`http://localhost:5050/api/User/${userId}/restore`, {
    method: "PATCH",
    headers: { Authorization: `Bearer ${token}` },
  })
    .then(async (res) => {
      const responseData = await res.json();
      if (!res.ok) {
        throw new Error(responseData.message || `HTTP ${res.status}`);
      }
      return responseData;
    })
    .then((res) => {
      toastr.success("User restored successfully!");

      $("#restoreUserModal").modal("hide");

      reloadCurrentPageData();
    })
    .catch((error) => {
      console.error("Restore user error:", error);
      toastr.error(error.message || "Failed to restore user!");
    });
}

function reloadCurrentPageData() {
  const currentPath = window.location.pathname;
  const currentPage = currentPath.split("/").pop();

  toastr.clear();

  setTimeout(() => {
    if (currentPage === "index.html" || currentPage === "") {
      if (typeof updateUserStatsDashboard === "function") {
        updateUserStatsDashboard();
      }
    } else if (currentPage === "active-users.html") {
      if (typeof loadActiveUsersTable === "function") {
        loadActiveUsersTable();
      }
    } else if (currentPage === "deactive-users.html") {
      if (typeof loadDeactiveUsersTable === "function") {
        loadDeactiveUsersTable();
      }
    } else {
      const dt_user_table = $(".datatables-users");
      if (dt_user_table.length && $.fn.DataTable.isDataTable(dt_user_table)) {
        dt_user_table.DataTable().ajax.reload(null, false);
      } else {
        if (typeof loadAllUsersTable === "function") {
          loadAllUsersTable();
        }
      }
    }
  }, 100);
}

function openEditUserModal(userId) {
  const token = localStorage.getItem("authToken");
  if (!userId || !token) return;
  fetch(`http://localhost:5050/api/User/${userId}`, {
    headers: { Authorization: `Bearer ${token}` },
  })
    .then((res) => res.json())
    .then((res) => {
      const user = res.data || res;
      if (!user || !user.id) {
        toastr.error("User not found!");
        return;
      }
      $("#editUserForm").data("userid", user.id);
      $("#editUserForm").data("original", {
        fullName: user.fullName || "",
        phoneNumber: user.phoneNumber || "",
        dateOfBirth: user.dateOfBirth ? user.dateOfBirth.split("T")[0] : "",
        address: user.address || "",
        bio: user.bio || "",
        status: user.status ? String(user.status) : "1",
        isVerified: !!user.isVerified,
      });
      $("#edit-username")
        .val(user.username || "")
        .prop("disabled", true);
      $("#edit-email")
        .val(user.email || "")
        .prop("disabled", true);
      $("#edit-fullName").val(user.fullName || "");
      $("#edit-phoneNumber").val(user.phoneNumber || "");
      $("#edit-dateOfBirth").val(
        user.dateOfBirth ? user.dateOfBirth.split("T")[0] : "",
      );
      $("#edit-address").val(user.address || "");
      $("#edit-bio").val(user.bio || "");
      $("#edit-status").val(user.status ? String(user.status) : "1");
      $("#edit-isVerified").prop("checked", !!user.isVerified);
      if (user.profilePicture) {
        $("#edit-profilePicture-preview").attr("src", user.profilePicture);
        $("#edit-profilePicture-container").show();
      } else {
        $("#edit-profilePicture-container").hide();
      }
      $("#edit-profilePicture").val("");
      if (cropper) {
        cropper.destroy();
        cropper = null;
      }
      window._editProfilePictureBase64 = null;
      selectedImageFile = null;
      window._profilePictureRemoved = false;
      $("#editUserModal").modal("show");
    })
    .catch(() => toastr.error("Failed to load user information!"));

  $("#editUserModal").on("hidden.bs.modal", function () {
    if (cropper) {
      cropper.destroy();
      cropper = null;
    }
    window._editProfilePictureBase64 = null;
    selectedImageFile = null;
    window._profilePictureRemoved = false;
  });
}

function openViewUserModal(userId) {
  const token = localStorage.getItem("authToken");
  if (!userId || !token) return;
  fetch(`http://localhost:5050/api/User/${userId}`, {
    headers: { Authorization: `Bearer ${token}` },
  })
    .then((res) => res.json())
    .then((res) => {
      const user = res.data || res;
      if (!user || !user.id) {
        toastr.error("User not found!");
        return;
      }
      $("#viewUserModal").data("userid", user.id);
      $(".user-username").text(user.username || "N/A");
      $(".user-fullname").text(user.fullName || "N/A");
      $(".user-email").text(user.email || "N/A");
      $(".user-phone").text(user.phoneNumber || "N/A");
      $(".user-lastlogin").text(
        user.lastLoginAt
          ? new Date(user.lastLoginAt).toLocaleString("en-GB", {
              year: "numeric",
              month: "2-digit",
              day: "2-digit",
              hour: "2-digit",
              minute: "2-digit",
              second: "2-digit",
            })
          : "Never",
      );
      $(".user-deletedat").text(
        user.deletedAt
          ? new Date(user.deletedAt).toLocaleString("en-GB", {
              year: "numeric",
              month: "2-digit",
              day: "2-digit",
              hour: "2-digit",
              minute: "2-digit",
              second: "2-digit",
            })
          : "N/A",
      );
      $(".user-address").text(user.address || "N/A");
      $(".user-dob").text(
        user.dateOfBirth
          ? new Date(user.dateOfBirth).toLocaleDateString("en-GB")
          : "N/A",
      );
      $(".user-verified").text(user.isVerified ? "Yes" : "No");
      $(".user-provider").text(user.loginProvider || "Local");
      $(".user-bio").text(user.bio || "No bio available");
      const $avatar = $("#view-user-avatar");
      const $fallback = $("#avatar-fallback");
      if (user.profilePicture && user.profilePicture.trim() !== "") {
        $avatar.attr("src", user.profilePicture).show();
        $fallback.hide();
      } else {
        $avatar.hide();
        const letter = (user.username || "").charAt(0).toUpperCase();
        const color = "#" + (((1 << 24) * Math.random()) | 0).toString(16);
        $fallback
          .text(letter)
          .css({ background: color, color: "#fff", display: "flex" });
        $fallback.show();
      }
      const statusBadge = $(".user-status-badge");
      statusBadge.html(getStatusBadge(user.status));
      $("#viewUserModal").modal("show");
    })
    .catch((err) => {
      toastr.error("Failed to load user information!");
    });
}
window.openViewUserModal = openViewUserModal;

$(document).on("click", "#viewUserModal .btn-edit-user", function () {
  const userId = $("#viewUserModal").data("userid");
  if (userId) {
    $("#viewUserModal").modal("hide");
    setTimeout(() => openEditUserModal(userId), 300);
  }
});

$(document).on("click", "#viewUserModal .btn-delete-user", function () {
  const userId = $("#viewUserModal").data("userid");
  if (userId) {
    $("#viewUserModal").modal("hide");
    setTimeout(() => openDeleteUserModal(userId), 300);
  }
});

$(document).on("click", "#viewUserModal .btn-restore-user", function () {
  const userId = $("#viewUserModal").data("userid");
  if (userId) {
    $("#viewUserModal").modal("hide");
    setTimeout(() => openRestoreUserModal(userId), 300);
  }
});

$(document).on("click", "#saveEditUserBtn", function () {
  const userId = $("#editUserForm").data("userid");
  if (userId) {
    handleUpdateUser(userId);
  }
});

$(document).on("click", "#confirmDeleteUser", function () {
  const userId = $("#deleteUserModal").data("userid");
  if (userId) {
    deleteUser(userId);
    $("#deleteUserModal").modal("hide");
  }
});

$(document).on("click", "#confirmRestoreUser", function () {
  const userId = $("#restoreUserModal").data("userid");
  if (userId) {
    restoreUser(userId);
    $("#restoreUserModal").modal("hide");
  }
});

$(document).on("submit", "#editUserForm", function (e) {
  e.preventDefault();
  const userId = $(this).data("userid");
  if (userId) {
    handleUpdateUser(userId);
  }
});

$(document)
  .off("click", ".view-user")
  .on("click", ".view-user", function (e) {
    e.stopPropagation();
    const userId = $(this).closest("tr").attr("data-userid");
    if (userId) openViewUserModal(userId);
  });

$(document)
  .off("click", ".edit-user")
  .on("click", ".edit-user", function (e) {
    e.stopPropagation();
    const userId = $(this).closest("tr").attr("data-userid");
    if (userId) openEditUserModal(userId);
  });

$(document)
  .off("click", ".delete-user")
  .on("click", ".delete-user", function (e) {
    e.stopPropagation();
    const userId = $(this).closest("tr").attr("data-userid");
    if (userId) openDeleteUserModal(userId);
  });

$(document)
  .off("click", ".restore-user")
  .on("click", ".restore-user", function (e) {
    e.stopPropagation();
    const userId = $(this).closest("tr").attr("data-userid");
    if (userId) openRestoreUserModal(userId);
  });

$(document)
  .off("click", ".datatables-users tbody tr")
  .on("click", ".datatables-users tbody tr", function (e) {
    if (
      $(e.target).closest(
        ".edit-user, .delete-user, .restore-user, .view-user, .btn",
      ).length
    )
      return;
    const userId = $(this).attr("data-userid");
    if (userId) openViewUserModal(userId);
  });

$(document).on("click", "#add-user-btn", function () {
  $("#offcanvasAddUser").offcanvas("show");
});

$(document).on("submit", "#addNewUserForm", function (e) {
  e.preventDefault();
  handleAddUser();
});
$(document).on("click", '.breadcrumb-item a[href="index.html"]', function (e) {
  e.preventDefault();
  window.location.href = "index.html";
});

function updateUserStatsDashboard() {
  const token = localStorage.getItem("authToken");
  if (!token) return;

  fetch("http://localhost:5050/api/User?includeDeleted=true", {
    headers: { Authorization: "Bearer " + token },
  })
    .then((res) => res.json())
    .then((json) => {
      if (!json || !Array.isArray(json.data)) {
        $("#total-users").text(0);
        $("#active-users").text(0);
        $("#inactive-users").text(0);
        $("#banned-users").text(0);
        return;
      }
      const users = json.data;
      let total = users.length,
        active = 0,
        inactive = 0,
        banned = 0;
      users.forEach((u) => {
        if (u.status === 1 || u.status === "Active") active++;
        else if (u.status === 2 || u.status === "Inactive") inactive++;
        else if (u.status === 4 || u.status === "Banned") banned++;
      });
      $("#total-users").text(total);
      $("#active-users").text(active);
      $("#inactive-users").text(inactive);
      $("#banned-users").text(banned);
    })
    .catch((error) => {
      console.error("Failed to update dashboard stats:", error);
      $("#total-users").text(0);
      $("#active-users").text(0);
      $("#inactive-users").text(0);
      $("#banned-users").text(0);
    });
}

function getStatusBadge(status) {
  switch (status) {
    case 1:
    case "Active":
      return '<span class="badge bg-label-success">Active</span>';
    case 2:
    case "Inactive":
      return '<span class="badge bg-label-secondary">Inactive</span>';
    case 3:
    case "Suspended":
      return '<span class="badge bg-label-warning">Suspended</span>';
    case 4:
    case "Banned":
      return '<span class="badge bg-label-danger">Banned</span>';
    default:
      return '<span class="badge bg-label-secondary">Unknown</span>';
  }
}

window.openDeleteUserModal = function (userId) {
  const token = localStorage.getItem("authToken");
  if (!userId || !token) return;
  fetch(`http://localhost:5050/api/User/${userId}`, {
    headers: { Authorization: `Bearer ${token}` },
  })
    .then((res) => res.json())
    .then((res) => {
      const user = res.data || res;
      if (!user || !user.id) {
        toastr.error("User not found!");
        return;
      }
      $("#deleteUserModal").data("userid", user.id);
      $(".delete-user-username").text(user.username || "N/A");
      $(".delete-user-fullname").text(user.fullName || "N/A");
      $(".delete-user-email").text(user.email || "N/A");
      $(".delete-user-phone").text(user.phoneNumber || "N/A");
      $(".delete-user-status").html(getStatusBadge(user.status));
      $(".delete-user-lastlogin").text(
        user.lastLoginAt
          ? new Date(user.lastLoginAt).toLocaleString("en-GB", {
              year: "numeric",
              month: "2-digit",
              day: "2-digit",
              hour: "2-digit",
              minute: "2-digit",
              second: "2-digit",
            })
          : "Never",
      );
      $(".delete-user-address").text(user.address || "N/A");
      $(".delete-user-created").text(
        user.createdAt
          ? new Date(user.createdAt).toLocaleString("en-GB", {
              year: "numeric",
              month: "2-digit",
              day: "2-digit",
              hour: "2-digit",
              minute: "2-digit",
              second: "2-digit",
            })
          : "N/A",
      );
      $("#deleteUserModal").modal("show");
    })
    .catch(() => toastr.error("Failed to load user information!"));
};

window.openRestoreUserModal = function (userId) {
  const token = localStorage.getItem("authToken");
  if (!userId || !token) return;
  fetch(`http://localhost:5050/api/User/${userId}`, {
    headers: { Authorization: `Bearer ${token}` },
  })
    .then((res) => res.json())
    .then((res) => {
      const user = res.data || res;
      if (!user || !user.id) {
        toastr.error("User not found!");
        return;
      }
      $("#restoreUserModal").data("userid", user.id);
      $(".restore-user-username").text(user.username || "N/A");
      $(".restore-user-fullname").text(user.fullName || "N/A");
      $(".restore-user-email").text(user.email || "N/A");
      $(".restore-user-phone").text(user.phoneNumber || "N/A");
      $(".restore-user-status").html(getStatusBadge(user.status));
      $(".restore-user-deletedat").text(
        user.deletedAt
          ? new Date(user.deletedAt).toLocaleString("en-GB", {
              year: "numeric",
              month: "2-digit",
              day: "2-digit",
              hour: "2-digit",
              minute: "2-digit",
              second: "2-digit",
            })
          : "N/A",
      );
      $(".restore-user-address").text(user.address || "N/A");
      $(".restore-user-created").text(
        user.createdAt
          ? new Date(user.createdAt).toLocaleString("en-GB", {
              year: "numeric",
              month: "2-digit",
              day: "2-digit",
              hour: "2-digit",
              minute: "2-digit",
              second: "2-digit",
            })
          : "N/A",
      );
      $("#restoreUserModal").modal("show");
    })
    .catch(() => toastr.error("Failed to load user information!"));
};

// Helper to generate SVG avatar as data URL
function generateLetterAvatarFromUser(user) {
  let letter = "U";
  if (user && user.email && user.email.trim() !== "") {
    letter = user.email.trim().charAt(0).toUpperCase();
  } else if (user && user.username && user.username.trim() !== "") {
    letter = user.username.trim().charAt(0).toUpperCase();
  } else if (user && user.fullName && user.fullName.trim() !== "") {
    letter = user.fullName.trim().charAt(0).toUpperCase();
  }
  const color = "#" + (((1 << 24) * Math.random()) | 0).toString(16);
  const svg = `<svg width='40' height='40' xmlns='http://www.w3.org/2000/svg'><circle cx='20' cy='20' r='20' fill='${color}'/><text x='50%' y='50%' text-anchor='middle' dy='.35em' font-family='Arial' font-size='20' fill='#fff'>${letter}</text></svg>`;
  return "data:image/svg+xml;base64," + btoa(unescape(encodeURIComponent(svg)));
}

// Patch DataTables avatar rendering to use the same logic
if (typeof window.getUserTableColumnDefs === "function") {
  const oldDefs = window.getUserTableColumnDefs;
  window.getUserTableColumnDefs = function (isDeactive) {
    const defs = oldDefs(isDeactive);
    defs.forEach((def) => {
      if (def.targets === 0) {
        def.render = function (data, type, full) {
          if (full.profilePicture && full.profilePicture.trim() !== "") {
            return `<img src="${full.profilePicture}" alt="avatar" class="rounded-circle" style="width:36px;height:36px;object-fit:cover;">`;
          } else {
            const svg = generateLetterAvatarFromUser(full);
            return `<img src="${svg}" alt="avatar" class="rounded-circle" style="width:36px;height:36px;object-fit:cover;">`;
          }
        };
      }
    });
    return defs;
  };
}

// Profile page functionality
async function loadUserProfile() {
  try {
    const userInfo = window.adminAuth.getCurrentUserInfo();
    if (!userInfo) {
      toastr.error("User information not found");
      return;
    }

    const response = await fetch(
      `http://localhost:5050/api/User/${userInfo.id}`,
      {
        headers: window.adminAuth.getAuthHeaders(),
      },
    );

    if (response.ok) {
      const data = await response.json();
      const user = data.data;

      // Populate profile form
      $("#profile-fullname").val(user.fullName || "");
      $("#profile-email").val(user.email || "");
      $("#profile-phone").val(user.phone || "");

      // Set profile picture
      if (user.profilePicture && user.profilePicture.trim() !== "") {
        $("#profile-picture-preview").attr("src", user.profilePicture).show();
      } else {
        // Generate letter avatar
        const letter = (user.fullName || user.username || "U")
          .charAt(0)
          .toUpperCase();
        const color = "#" + (((1 << 24) * Math.random()) | 0).toString(16);
        const svg = `<svg width='100' height='100' xmlns='http://www.w3.org/2000/svg'><circle cx='50' cy='50' r='50' fill='${color}'/><text x='50%' y='50%' text-anchor='middle' dy='.35em' font-family='Arial' font-size='40' fill='#fff'>${letter}</text></svg>`;
        const dataUrl =
          "data:image/svg+xml;base64," +
          btoa(unescape(encodeURIComponent(svg)));
        $("#profile-picture-preview").attr("src", dataUrl).show();
      }
    } else {
      toastr.error("Failed to load user profile");
    }
  } catch (error) {
    console.error("Error loading user profile:", error);
    toastr.error("Error loading user profile");
  }
}

async function saveUserProfile(formData) {
  try {
    const userInfo = window.adminAuth.getCurrentUserInfo();
    if (!userInfo) {
      toastr.error("User information not found");
      return false;
    }

    const response = await fetch(
      `http://localhost:5050/api/User/${userInfo.id}`,
      {
        method: "PUT",
        headers: {
          ...window.adminAuth.getAuthHeaders(),
          "Content-Type": "application/json",
        },
        body: JSON.stringify(formData),
      },
    );

    if (response.ok) {
      toastr.success("Profile updated successfully");
      return true;
    } else {
      const errorData = await response.json();
      toastr.error(errorData.message || "Failed to update profile");
      return false;
    }
  } catch (error) {
    console.error("Error saving user profile:", error);
    toastr.error("Error saving profile");
    return false;
  }
}

// Settings page functionality
function loadUserSettings() {
  try {
    // Load settings from localStorage
    const settings = JSON.parse(localStorage.getItem("userSettings") || "{}");

    $("#settings-notifications").prop(
      "checked",
      settings.notifications !== false,
    );
    $("#settings-language").val(settings.language || "en");
    $("#settings-darkmode").prop("checked", settings.darkMode === true);

    toastr.success("Settings loaded");
  } catch (error) {
    console.error("Error loading settings:", error);
    toastr.error("Error loading settings");
  }
}

function saveUserSettings() {
  try {
    const settings = {
      notifications: $("#settings-notifications").is(":checked"),
      language: $("#settings-language").val(),
      darkMode: $("#settings-darkmode").is(":checked"),
    };

    localStorage.setItem("userSettings", JSON.stringify(settings));
    toastr.success("Settings saved successfully");

    // Apply settings
    if (settings.darkMode) {
      $("html").addClass("dark-style");
    } else {
      $("html").removeClass("dark-style");
    }

    return true;
  } catch (error) {
    console.error("Error saving settings:", error);
    toastr.error("Error saving settings");
    return false;
  }
}

// Initialize page-specific functionality
function initializePageFunctionality() {
  const currentPage = window.location.pathname.split("/").pop();

  switch (currentPage) {
    case "pages-profile-user.html":
      // Profile page
      loadUserProfile();

      // Handle profile form submission
      $("#profile-form").on("submit", async function (e) {
        e.preventDefault();

        const formData = {
          fullName: $("#profile-fullname").val(),
          phone: $("#profile-phone").val(),
        };

        // Handle profile picture upload
        const fileInput = $("#profile-picture-input")[0];
        if (fileInput.files.length > 0) {
          const file = fileInput.files[0];
          const reader = new FileReader();
          reader.onload = async function (e) {
            formData.profilePicture = e.target.result;
            await saveUserProfile(formData);
          };
          reader.readAsDataURL(file);
        } else {
          await saveUserProfile(formData);
        }
      });

      // Handle profile picture preview
      $("#profile-picture-input").on("change", function () {
        const file = this.files[0];
        if (file) {
          const reader = new FileReader();
          reader.onload = function (e) {
            $("#profile-picture-preview").attr("src", e.target.result);
          };
          reader.readAsDataURL(file);
        }
      });
      break;

    case "my-profile.html":
      loadUserProfile();

      $("#profile-form").on("submit", async function (e) {
        e.preventDefault();

        const formData = {
          fullName: $("#profile-fullname").val(),
          phone: $("#profile-phone").val(),
        };

        const fileInput = $("#profile-picture-input")[0];
        if (fileInput.files.length > 0) {
          const file = fileInput.files[0];
          const reader = new FileReader();
          reader.onload = async function (e) {
            formData.profilePicture = e.target.result;
            await saveUserProfile(formData);
          };
          reader.readAsDataURL(file);
        } else {
          await saveUserProfile(formData);
        }
      });

      $("#profile-picture-input").on("change", function () {
        const file = this.files[0];
        if (file) {
          const reader = new FileReader();
          reader.onload = function (e) {
            $("#profile-picture-preview").attr("src", e.target.result);
          };
          reader.readAsDataURL(file);
        }
      });
      break;

    case "notifications.html":
      loadUserSettings();

      $("#settings-form").on("submit", function (e) {
        e.preventDefault();
        saveUserSettings();
      });
      break;

    case "faq.html":
      break;
  }
}

$(document).on("shown.bs.modal", "#cropImageModal", function () {
  setTimeout(() => {
    updateCircleOverlay();
  }, 150);
});

// --- Change Password Logic for security.html ---
document.addEventListener("DOMContentLoaded", function () {
  const form = document.getElementById("security-form");
  if (form) {
    form.addEventListener("submit", async function (e) {
      e.preventDefault();
      const oldPwd = document.getElementById("old-password").value.trim();
      const newPwd = document.getElementById("new-password").value.trim();
      const confirmPwd = document.getElementById("confirm-password").value.trim();
      if (!oldPwd || !newPwd || !confirmPwd) {
        toastr.error("All fields are required");
        return;
      }
      if (newPwd.length < 6) {
        toastr.error("New password must be at least 6 characters");
        return;
      }
      if (newPwd !== confirmPwd) {
        toastr.error("Passwords do not match");
        return;
      }
      try {
        const token = localStorage.getItem("authToken");
        if (!token) {
          toastr.error("Authentication required!");
          return;
        }
        const res = await fetch(
          "http://localhost:5001/api/Auth/change-password",
          {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
              Authorization: "Bearer " + token,
            },
            body: JSON.stringify({
              currentPassword: oldPwd,
              newPassword: newPwd,
              confirmPassword: confirmPwd,
            }),
          },
        );
        const data = await res.json();
        if (res.ok && data.success !== false) {
          toastr.success("Password changed successfully");
          form.reset();
        } else {
          toastr.error(data.message || "Change password failed");
        }
      } catch (err) {
        toastr.error("Change password failed");
      }
    });
  }
});
// --- End Change Password Logic ---
