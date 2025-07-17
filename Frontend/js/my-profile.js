// My Profile JavaScript
$(document).ready(async function () {
  let attempts = 0;
  const maxAttempts = 10;

  const waitForAdminAuth = async () => {
    if (
      window.adminAuth &&
      typeof window.adminAuth.updateUserProfileDisplay === "function"
    ) {
      await window.adminAuth.updateUserProfileDisplay();
    } else if (attempts < maxAttempts) {
      attempts++;
      setTimeout(waitForAdminAuth, 100);
    }
  };

  waitForAdminAuth();
});

document.addEventListener("DOMContentLoaded", async function () {
  let attempts = 0;
  const maxAttempts = 20;

  const waitForAdminAuth = async () => {
    if (
      window.adminAuth &&
      typeof window.adminAuth.updateUserProfileDisplay === "function"
    ) {
      await window.adminAuth.updateUserProfileDisplay();
    } else if (attempts < maxAttempts) {
      attempts++;
      setTimeout(waitForAdminAuth, 100);
    }
  };

  waitForAdminAuth();

  function getToken() {
    if (typeof window.getToken === "function") {
      return window.getToken();
    }
    return localStorage.getItem("authToken");
  }

  function parseJwt(token) {
    if (typeof window.parseJwt === "function") {
      return window.parseJwt(token);
    }
    if (!token) return null;
    try {
      return JSON.parse(
        atob(token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")),
      );
    } catch {
      return null;
    }
  }

  function getCurrentUserInfo() {
    const token = getToken();
    const payload = parseJwt(token);
    if (!payload) return null;
    return {
      id: payload.sub,
      email: payload.email,
      fullName: payload.name,
    };
  }

  function getAuthHeaders() {
    const token = getToken();
    return token
      ? { Authorization: "Bearer " + token, "Content-Type": "application/json" }
      : { "Content-Type": "application/json" };
  }

  const API_BASE = "http://localhost:5050/api/User";

  const form = document.getElementById("editUserForm");
  const removeProfilePictureBtn = document.getElementById(
    "remove-profile-picture",
  );
  const profilePictureInput = document.getElementById("edit-profilePicture");
  const cropImageBtn = document.getElementById("cropImageBtn");

  async function loadUserInfo() {
    let user = getCurrentUserInfo();
    if (!user || !user.id) {
      let msg =
        window.i18next && typeof window.i18next.t === "function"
          ? window.i18next.t("userNotFoundOrInvalidToken")
          : "User not found or invalid token.";
      showToastr(msg, "error");
      setTimeout(() => {
        window.location.href = "/auth/login.html";
      }, 1000);
      return;
    }
    try {
      const res = await fetch(`${API_BASE}/${user.id}`, {
        headers: getAuthHeaders(),
      });
      const data = await res.json();
      if (data.success && data.data) {
        const u = data.data;
        $("#edit-username")
          .val(u.username || "")
          .prop("disabled", true);
        $("#edit-email")
          .val(u.email || "")
          .prop("disabled", true);
        $("#edit-fullName").val(u.fullName || "");
        $("#edit-phoneNumber").val(u.phoneNumber || "");
        $("#edit-dateOfBirth").val(
          u.dateOfBirth ? u.dateOfBirth.split("T")[0] : "",
        );
        $("#edit-address").val(u.address || "");
        $("#edit-bio").val(u.bio || "");
        if (u.profilePicture) {
          $("#edit-profilePicture-preview").attr("src", u.profilePicture);
          $("#edit-profilePicture-container").show();
        } else {
          if (typeof window.generateLetterAvatarFromUser === "function") {
            $("#edit-profilePicture-preview").attr(
              "src",
              window.generateLetterAvatarFromUser(u),
            );
            $("#edit-profilePicture-container").show();
          } else {
            $("#edit-profilePicture-container").hide();
          }
        }
        $("#edit-profilePicture").val("");
        window._editProfilePictureBase64 = null;
        window._profilePictureRemoved = false;
        $("#editUserForm").data("original", {
          fullName: u.fullName || "",
          phoneNumber: u.phoneNumber || "",
          dateOfBirth: u.dateOfBirth || "",
          address: u.address || "",
          bio: u.bio || "",
        });
        $("#editUserForm").data("userid", u.id);

        // Update avatar and user info in navbar after loading user data
        if (
          window.adminAuth &&
          typeof window.adminAuth.updateUserProfileDisplay === "function"
        ) {
          await window.adminAuth.updateUserProfileDisplay();
        }
      } else {
        showToastr(
          data.message || window.i18next.t("failedToLoadUserInfo"),
          "error",
        );
      }
    } catch (e) {
      showToastr(window.i18next.t("failedToLoadUserInfo"), "error");
    }
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
          showToastr(window.i18next.t("pleaseSelectValidImageFile"), "error");
          this.value = "";
          return;
        }
        if (file.size > 5 * 1024 * 1024) {
          showToastr(window.i18next.t("imageSizeMustBeLessThan5MB"), "error");
          this.value = "";
          return;
        }
        if (file.size < 10 * 1024) {
          showToastr(window.i18next.t("imageSizeIsVerySmall"), "warning");
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

  $(document)
    .off("click", "#cropImageBtn")
    .on("click", "#cropImageBtn", function () {
      if (!cropper || !cropperReady) {
        showToastr(window.i18next.t("cropperNotReady"), "error");
        return;
      }
      let cropBox = cropper.getCropBoxData();
      const imageData = cropper.getImageData();
      if (!cropBox || cropBox.width <= 0 || cropBox.height <= 0) {
        showToastr(window.i18next.t("cropAreaInvalid"), "error");
        return;
      }
      if (
        imageData &&
        (cropBox.width > imageData.naturalWidth ||
          cropBox.height > imageData.naturalHeight)
      ) {
        showToastr(window.i18next.t("cropAreaLargerThanImage"), "error");
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
        if (!canvas) throw new Error(window.i18next.t("canvasIsNull"));
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
          const msg =
            window.i18next && typeof window.i18next.t === "function"
              ? window.i18next.t("profilePictureCroppedSuccessfully")
              : "Profile picture cropped successfully!";
          showToastr(msg, "success");
          if (window.adminAuth) {
            window.adminAuth.updateUserProfileDisplay();
          }
        }, 200);
      } catch (error) {
        window._editProfilePictureBase64 = null;
        $("#crop-avatar-preview, #edit-profilePicture-preview").attr("src", "");
        $("#edit-profilePicture-container, #profile-picture-preview").hide();
        showToastr(
          error.message || window.i18next.t("failedToCropImage"),
          "error",
        );
      }
    });

  $(document)
    .off("click", "#remove-profile-picture")
    .on("click", "#remove-profile-picture", function () {
      $("#edit-profilePicture-preview").attr("src", "");
      $("#edit-profilePicture-container").hide();
      $("#edit-profilePicture").val("");
      window._editProfilePictureBase64 = null;
      selectedImageFile = null;
      window._profilePictureRemoved = true;

      if (cropper) {
        cropper.destroy();
        cropper = null;
      }

      showToastr(window.i18next.t("profilePictureRemoved"), "info");
    });

  $(form)
    .off("submit")
    .on("submit", async function (e) {
      e.preventDefault();

      const userId = $(form).data("userid");
      if (!userId) {
        showToastr(window.i18next.t("userIdNotFound"), "error");
        return;
      }

      const user = getCurrentUserInfo();
      if (!user) {
        showToastr(window.i18next.t("userNotFound"), "error");
        return;
      }

      const formData = new FormData(form);
      let dateOfBirth = formData.get("dateOfBirth");

      const data = {
        fullName: formData.get("fullName")?.trim(),
        phoneNumber: formData.get("phoneNumber")?.trim(),
        address: formData.get("address")?.trim(),
        bio: formData.get("bio")?.trim(),
      };

      if (dateOfBirth && dateOfBirth.trim() !== "") {
        const today = new Date();
        if (dateOfBirth > today.toISOString().split("T")[0]) {
          showToastr(window.i18next.t("dateOfBirthCannotBeInFuture"), "error");
          return;
        }
        data.dateOfBirth = new Date(dateOfBirth).toISOString();
      }

      if (window._editProfilePictureBase64) {
        data.profilePicture = window._editProfilePictureBase64;
      } else if (selectedImageFile) {
        showToastr(
          window.i18next.t("pleaseCropYourProfilePictureBeforeSaving"),
          "warning",
        );
        return;
      } else if (window._profilePictureRemoved) {
        data.profilePicture = null;
      }

      Object.keys(data).forEach((key) => {
        if (data[key] === null || data[key] === undefined || data[key] === "") {
          delete data[key];
        }
      });

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
        let oldVal = original[key] || "";
        let newVal = data[key] || "";

        if (key === "dateOfBirth") {
          if (oldVal) oldVal = oldVal.split("T")[0];
          if (newVal) newVal = newVal.split("T")[0];
        }

        if (String(oldVal) !== String(newVal)) {
          changed = true;
          break;
        }
      }

      if (!changed) {
        showToastr(window.i18next.t("noInformationChanged"), "warning");
        return;
      }

      if (data.phoneNumber && !/^[0-9]{10,11}$/.test(data.phoneNumber)) {
        showToastr(
          window.i18next.t("phoneNumberMustBe10To11DigitsAndOnlyNumbers"),
          "error",
        );
        return;
      }

      if (!data.fullName) {
        showToastr(window.i18next.t("fullNameIsRequired"), "error");
        return;
      }

      const token = getToken();
      if (!token) {
        showToastr(window.i18next.t("authenticationRequired"), "error");
        return;
      }

      try {
        const res = await fetch(`${API_BASE}/${userId}`, {
          method: "PUT",
          headers: getAuthHeaders(),
          body: JSON.stringify(data),
        });

        const responseData = await res.json();

        if (!res.ok) {
          if (responseData.errors) {
            Object.keys(responseData.errors).forEach((field) => {
              const messages = responseData.errors[field];
              if (Array.isArray(messages)) {
                messages.forEach((msg) =>
                  showToastr(`${field}: ${msg}`, "error"),
                );
              } else {
                showToastr(`${field}: ${messages}`, "error");
              }
            });
          } else if (responseData.message) {
            showToastr(responseData.message, "error");
          } else {
            showToastr(window.i18next.t("failedToUpdateProfile"), "error");
          }
          return;
        }

        showToastr(window.i18next.t("profileUpdatedSuccessfully"), "success");

        window._editProfilePictureBase64 = null;
        selectedImageFile = null;
        window._profilePictureRemoved = false;

        if (cropper) {
          cropper.destroy();
          cropper = null;
        }

        await loadUserInfo();

        if (window.adminAuth) {
          await window.adminAuth.updateUserProfileDisplay();
        }
      } catch (err) {
        showToastr(err.message || window.i18next.t("updateFailed"), "error");
      }
    });

  // Initialize deactivate account button state
  function updateDeactivateButtonState() {
    const checkbox = document.getElementById("accountActivation");
    const deactivateBtn = document.querySelector(".deactivate-account");
    if (checkbox && deactivateBtn) {
      deactivateBtn.disabled = !checkbox.checked;
      if (checkbox.checked) {
        deactivateBtn.classList.remove("btn-secondary");
        deactivateBtn.classList.add("btn-danger");
      } else {
        deactivateBtn.classList.remove("btn-danger");
        deactivateBtn.classList.add("btn-secondary");
      }
    }
  }

  // Add event listener for checkbox
  $(document)
    .off("change", "#accountActivation")
    .on("change", "#accountActivation", function () {
      updateDeactivateButtonState();
    });

  // Initialize button state on page load
  updateDeactivateButtonState();

  // Load user information
  await loadUserInfo();

  // Function to generate letter avatar
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
    return (
      "data:image/svg+xml;base64," + btoa(unescape(encodeURIComponent(svg)))
    );
  }

  // Make function globally available
  window.generateLetterAvatarFromUser = generateLetterAvatarFromUser;

  $(document)
    .off("click", ".deactivate-account")
    .on("click", ".deactivate-account", async function (e) {
      e.preventDefault();

      // Check if checkbox is checked
      const checkbox = document.getElementById("accountActivation");
      if (!checkbox || !checkbox.checked) {
        showToastr(
          window.i18next.t("pleaseConfirmAccountDeactivation"),
          "warning",
        );
        return;
      }

      if (
        !confirm(window.i18next.t("areYouSureYouWantToDeactivateYourAccount"))
      ) {
        return;
      }

      const user = getCurrentUserInfo();
      if (!user) {
        showToastr(window.i18next.t("userNotFound"), "error");
        return;
      }

      try {
        const res = await fetch(`${API_BASE}/${user.id}`, {
          method: "DELETE",
          headers: getAuthHeaders(),
        });

        const data = await res.json();

        if (res.ok && data.success) {
          showToastr(window.i18next.t("accountDeactivated"), "success");
          localStorage.removeItem("authToken");
          setTimeout(() => {
            window.location.href = "/auth/login.html";
          }, 1500);
        } else {
          showToastr(
            data.message || window.i18next.t("deactivationFailed"),
            "error",
          );
        }
      } catch (err) {
        showToastr(
          err.message || window.i18next.t("deactivationFailed"),
          "error",
        );
      }
    });

  // --- CropperJS integration ---
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
          let cropBox, imageData;
          try {
            cropBox = cropper.getCropBoxData();
            imageData = cropper.getImageData();
          } catch (e) {
            showToastr(window.i18next.t("failedToInitializeCropper"), "error");
            $("#cropImageModal").modal("hide");
            return;
          }
          if (!imageData || !cropBox) return setTimeout(waitForCropBox, 30);
          // Set cropBox mặc định nhỏ hơn ảnh (80%) và ở chính giữa nếu lần đầu
          if (tryCount === 1) {
            const boxSize = Math.floor(
              Math.min(imageData.naturalWidth, imageData.naturalHeight) * 0.8,
            );
            cropper.setCropBoxData({
              width: boxSize,
              height: boxSize,
              left: imageData.left + (imageData.naturalWidth - boxSize) / 2,
              top: imageData.top + (imageData.naturalHeight - boxSize) / 2,
            });
            setTimeout(waitForCropBox, 30);
            return;
          }
          // Nếu cropBox vượt biên, tự động điều chỉnh về vùng hợp lệ
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
          if (
            cropBox.left + cropBox.width >
            imageData.left + imageData.naturalWidth
          ) {
            newLeft = imageData.left + imageData.naturalWidth - cropBox.width;
            adjusted = true;
          }
          if (
            cropBox.top + cropBox.height >
            imageData.top + imageData.naturalHeight
          ) {
            newTop = imageData.top + imageData.naturalHeight - cropBox.height;
            adjusted = true;
          }
          if (adjusted) {
            cropper.setCropBoxData({
              width: cropBox.width,
              height: cropBox.height,
              left: newLeft,
              top: newTop,
            });
            setTimeout(waitForCropBox, 30);
            return;
          }
          // Nếu ảnh quá nhỏ (bé hơn 100x100), báo lỗi và không cho crop
          if (imageData.naturalWidth < 100 || imageData.naturalHeight < 100) {
            showToastr(window.i18next.t("imageTooSmall"), "error");
            $("#cropImageModal").modal("hide");
            return;
          }
          // Nếu thử quá 10 lần mà cropBox vẫn không hợp lệ, báo lỗi
          if (
            tryCount > 10 &&
            (!cropBox || cropBox.width <= 0 || cropBox.height <= 0)
          ) {
            showToastr(window.i18next.t("failedToInitializeCropper"), "error");
            $("#cropImageModal").modal("hide");
            return;
          }
          // Lưu lại cropBox.width ban đầu để tính zoom đúng chuẩn
          if (!initialCropBoxWidth) initialCropBoxWidth = cropBox.width;
          // maxZoom là tỉ lệ nhỏ nhất để cropBox vừa khít ảnh
          let maxZoom = Math.min(
            Math.floor((initialCropBoxWidth / 10) * 100) / 100, // không cho nhỏ hơn 10px
            imageData.naturalWidth / initialCropBoxWidth,
            3,
          );
          maxZoom = Math.max(1, maxZoom);
          cropperReady = true;
          updateZoomSlider(maxZoom);
          updateCircleOverlay();
          updateAvatarPreview();
          // Disable slider nếu maxZoom=1
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
            // Tính lại cropBox width theo initialCropBoxWidth
            if (initialCropBoxWidth) {
              const newWidth = initialCropBoxWidth / currentZoom;
              const cropBox = cropper.getCropBoxData();
              cropper.setCropBoxData({
                width: newWidth,
                height: newWidth,
                left: cropBox.left,
                top: cropBox.top,
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
                top: cropBox.top,
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
                top: cropBox.top,
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
        showToastr(window.i18next.t("failedToLoadImage"), "error");
        $("#cropImageModal").modal("hide");
      },
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
      if (minLabel) minLabel.textContent = window.i18next.t("zoom100");
      if (maxLabel && maxZoom)
        maxLabel.textContent = `${Math.round(maxZoom * 100)}%`;
    }
  }

  function ensureCropBoxInBounds() {
    if (!cropper) return;
    const cropBox = cropper.getCropBoxData();
    const imageData = cropper.getImageData();
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
    if (
      cropBox.left + cropBox.width >
      imageData.left + imageData.naturalWidth
    ) {
      newLeft = imageData.left + imageData.naturalWidth - cropBox.width;
      adjusted = true;
    }
    if (
      cropBox.top + cropBox.height >
      imageData.top + imageData.naturalHeight
    ) {
      newTop = imageData.top + imageData.naturalHeight - cropBox.height;
      adjusted = true;
    }
    if (adjusted) {
      cropper.setCropBoxData({
        width: cropBox.width,
        height: cropBox.height,
        left: newLeft,
        top: newTop,
      });
    }
  }

  function updateZoomDisplay(zoom) {
    const percentage = Math.round((zoom || 1) * 100);
    const zoomDisplay = document.querySelector(".zoom-percentage");
    if (zoomDisplay) {
      zoomDisplay.textContent = `${percentage}%`;
    }
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
      // Đảm bảo cropBox nằm trong vùng ảnh
      if (
        cropBox.left < imageData.left ||
        cropBox.top < imageData.top ||
        cropBox.left + cropBox.width >
          imageData.left + imageData.naturalWidth ||
        cropBox.top + cropBox.height > imageData.top + imageData.naturalHeight
      ) {
        ensureCropBoxInBounds();
        cropBox = cropper.getCropBoxData();
      }
      if (!cropBox || cropBox.width <= 0 || cropBox.height <= 0) return;
      if (
        imageData &&
        (cropBox.width > imageData.naturalWidth ||
          cropBox.height > imageData.naturalHeight)
      ) {
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
        showToastr(window.i18next.t("pleaseSelectValidImageFile"), "error");
      }
    }
  });

  $(document).on("click", ".drag-drop-zone", function () {
    $("#edit-profilePicture, #profile-picture-input").click();
  });

  function handleImageFile(file) {
    if (file.size > 5 * 1024 * 1024) {
      showToastr(window.i18next.t("imageSizeMustBeLessThan5MB"), "error");
      return;
    }
    if (file.size < 10 * 1024) {
      showToastr(window.i18next.t("imageSizeIsVerySmall"), "warning");
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

  await loadUserInfo();

  if (
    window.adminAuth &&
    typeof window.adminAuth.updateUserProfileDisplay === "function"
  ) {
    window.adminAuth.updateUserProfileDisplay();
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
});
